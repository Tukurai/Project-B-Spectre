using Common.DAL;
using Common.DAL.Models;
using Common.Choices;
using Common.Services;
using Common.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Common.Enums;

namespace Guide_Spectre
{
    internal class Program
    {
        public static bool Running { get; set; } = true;
        public static bool ShowMenu { get; set; } = false;
        public static bool ShowSubMenu { get; set; } = false;
        public static User User { get; set; }
        public static ServiceProvider ServiceProvider { get; set; }
        public static LocalizationService Localization { get; set; }
        public static PromptService Prompts { get; set; }

        static void Main(string[] args)
        {
            // Setup services
            ServiceProvider = new ServiceCollection()
                .AddSingleton<DepotContext>()
                .AddSingleton<LocalizationService>()
                .AddSingleton<SettingsService>()
                .AddSingleton<PromptService>()
                .AddSingleton<TicketService>()
                .AddSingleton<TourService>()
                .AddSingleton<GroupService>()
                .AddSingleton<UserService>()
                .AddTransient<RemoveTicketTourGuideFlow>()
                .AddTransient<AddTicketTourGuideFlow>()
                .AddTransient<StartTourGuideFlow>()
                .BuildServiceProvider();

            // Get services
            Localization = ServiceProvider.GetService<LocalizationService>()!;
            Prompts = ServiceProvider.GetService<PromptService>()!;
            var userService = ServiceProvider.GetService<UserService>()!;

            // Setup context
            ServiceProvider.GetService<DepotContext>()!.LoadContext();

            // Menu loop
            while (Running)
            {
                var userpass = Prompts.AskUserpass();
                var hasAccess = userService.ValidateUserForRole(userpass, Role.Guide);
                User = userService.GetUser(userpass)!;
                AnsiConsole.Clear(); // Clear the console after the ticket has been scanned

                if (hasAccess.Valid)
                    ShowMenu = true;

                while (ShowMenu)
                    GuideMenu().NavigationAction();
            }

            Console.ReadLine();
        }

        private static object GetColor(int count, int maxSpots) => count == 0 ? "red" : count < maxSpots ? "green" : "blue";

        public static NavigationChoice GuideMenu()
        {
            var tourService = ServiceProvider.GetService<TourService>()!;
            var settingsService = ServiceProvider.GetService<SettingsService>()!;

            var options = new List<NavigationChoice>() { };
            var maxSpots = settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value;

            foreach (Tour tour in tourService.GetToursForToday(0, 2, 4))
            {
                var start = tour.Start.ToString("HH:mm");
                var state = tour.Departed ? Localization.Get("Tour_departed") : Localization.Get("Tour_not_departed");
                var registered = $"[{GetColor(tour.RegisteredTickets.Count, maxSpots)}]({tour.RegisteredTickets.Count}/{maxSpots})[/]";

                options.Add(new NavigationChoice(Localization.Get("Guide_tour_navigation_name",
                    replacementStrings: new() { start, state, registered }), () =>
                    {
                        ShowSubMenu = true;
                        while (ShowSubMenu)
                        {
                            TourMenu(tour).NavigationAction();
                        }
                    }));
            }

            options.Add(new(Localization.Get("Guide_close"), () => { CloseMenu(); }));

            return Prompts.GetMenu("Guide_title", "Guide_menu_more_options", options, User);
        }

        public static NavigationChoice TourMenu(Tour tour)
        {
            var settingsService = ServiceProvider.GetService<SettingsService>()!;
            var maxSpots = settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value;

            var ruleHeader = new Rule(Localization.Get("Guide_view_tour_title"));
            ruleHeader.Justification = Justify.Left;
            AnsiConsole.Write(ruleHeader);

            var start = tour.Start.ToString("HH:mm");
            var state = tour.Departed ? Localization.Get("Tour_departed") : Localization.Get("Tour_not_departed");
            var registered = $"[{GetColor(tour.RegisteredTickets.Count, maxSpots)}]({tour.RegisteredTickets.Count}/{maxSpots})[/]";

            AnsiConsole.MarkupLine(Localization.Get("Guide_view_tour_description", replacementStrings: new() { start, state, registered }));

            var ruleTickets = new Rule(Localization.Get("Guide_view_tour_tickets"));
            ruleTickets.Justification = Justify.Left;
            AnsiConsole.Write(ruleTickets);
            if (tour.RegisteredTickets.Any())
                AnsiConsole.Write(new Columns(tour.RegisteredTickets.Select(ticket => new Text(ticket.ToString(), new Style(Color.Green))).ToList()));
            else
                AnsiConsole.MarkupLine(Localization.Get("Guide_view_tour_no_tickets"));

            var emptyRule = new Rule();
            AnsiConsole.Write(emptyRule);

            var options = new List<NavigationChoice>() {
                new(Localization.Get("Guide_start_tour"), () => { GuideStartTour(tour); }),
                new(Localization.Get("Guide_add_ticket"), () => { GuideAddTicket(tour); }),
                new(Localization.Get("Guide_remove_ticket"), () => { GuideRemoveTicket(tour); }),
                new(Localization.Get("Guide_close"), () => { CloseMenu(subMenu: true); }),
            };

            return Prompts.GetMenu("Guide_submenu_title", "Guide_menu_more_options", options);
        }

        private static void GuideRemoveTicket(Tour tour)
        {
            AnsiConsole.Clear();
            var flow = ServiceProvider.GetService<RemoveTicketTourGuideFlow>()!;

            // Set tour into flow
            var setTourResult = flow.SetTour(tour);
            if (!setTourResult.Success)
            {
                flow.Rollback();
                CloseMenu(setTourResult.Message, false);
                return;
            }

            AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Remove_ticket_flow_title", "Remove_ticket_flow_current_column", "Remove_ticket_flow_ticket_remove_column", "blue", "red"));

            while (flow.Tour.RegisteredTickets.Any())
            {
                var ticketNumber = Prompts.AskTicketNumber();
                flow.RemoveTicket(ticketNumber);
                AnsiConsole.Clear();

                AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Add_ticket_flow_title", "Add_ticket_flow_ticket_current_column", "Add_ticket_flow_ticket_add_column", "blue", "red"));

                if (!flow.Tour.RegisteredTickets.Any() || !Prompts.AskConfirmation("Remove_ticket_flow_ask_more_tickets"))
                    break;
            }

            // Commit the flow.
            if (Prompts.AskConfirmation("Remove_ticket_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message);
                return;
            }

            flow.Rollback();
            CloseMenu(closeMenu: false, subMenu: true);
        }

        private static void GuideAddTicket(Tour tour)
        {
            AnsiConsole.Clear();
            var settingsService = ServiceProvider.GetService<SettingsService>()!;
            var flow = ServiceProvider.GetService<AddTicketTourGuideFlow>()!;

            // Set tour into flow
            var setTourResult = flow.SetTour(tour);
            if (!setTourResult.Success)
            {
                flow.Rollback();
                CloseMenu(setTourResult.Message, false);
                return;
            }

            AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Add_ticket_flow_title", "Add_ticket_flow_ticket_current_column", "Add_ticket_flow_ticket_add_column", "blue", "green"));

            while (flow.Tour.RegisteredTickets.Count < settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value)
            {
                var ticketNumber = Prompts.AskTicketNumber();
                flow.AddTicket(ticketNumber);
                AnsiConsole.Clear();

                AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Add_ticket_flow_title", "Add_ticket_flow_ticket_current_column", "Add_ticket_flow_ticket_add_column", "blue", "green"));

                if (flow.Tour.RegisteredTickets.Count >= settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value
                    || !Prompts.AskConfirmation("Add_ticket_flow_ask_more_tickets"))
                    break;
            }

            // Commit the flow.
            if (Prompts.AskConfirmation("Add_ticket_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message);
                return;
            }

            flow.Rollback();
            CloseMenu(closeMenu: false, subMenu: true);
        }

        private static void GuideStartTour(Tour tour)
        {
            AnsiConsole.Clear();
            var settingsService = ServiceProvider.GetService<SettingsService>()!;
            var flow = ServiceProvider.GetService<StartTourGuideFlow>()!;

            // Set tour into flow
            var setTourResult = flow.SetTour(tour);
            if (!setTourResult.Success)
            {
                flow.Rollback();
                CloseMenu(setTourResult.Message, false);
                return;
            }

            AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Start_tour_flow_title", "Start_tour_flow_ticket_todo_column", "Start_tour_flow_ticket_done_column", "blue", "green"));

            // Scan registered tickets
            ScanTickets(flow, flow.Tour.RegisteredTickets.Count, FlowStep.ScanRegistration);

            AnsiConsole.MarkupLine(Localization.Get("Start_tour_flow_scan_extra"));

            // Scan extra tickets
            ScanTickets(flow, settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value, FlowStep.ScanExtra);

            // Scan guide badge if not done yet.
            if (flow.GuideId == 0)
                flow.ScanBadge(Prompts.AskUserpass());

            // Commit the flow.
            if (Prompts.AskConfirmation("Start_tour_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message);
                return;
            }

            flow.Rollback();
            CloseMenu(closeMenu: false, subMenu: true);
        }

        public static void ScanTickets(StartTourGuideFlow flow, int maxTickets, FlowStep step)
        {
            while (flow.TicketBuffer.Count < maxTickets)
            {
                var ticketNumber = Prompts.AskTicketNumberOrUserpass();
                (bool Success, string Message) response;
                if (ticketNumber >= 10000000) // Guide & manager badges have an id with less then 8 digits. 
                    response = flow.AddScannedTicket(ticketNumber, flow.Step == FlowStep.ScanExtra);
                else
                    response = flow.ScanBadge(ticketNumber);

                if (!response.Success)
                {
                    AnsiConsole.WriteLine(response.Message);
                    Thread.Sleep(2000);
                }

                AnsiConsole.Clear();

                AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets.ToList(), flow.TicketBuffer, "Start_tour_flow_title", "Start_tour_flow_ticket_todo_column", "Start_tour_flow_ticket_done_column", "blue", "green"));

                if (flow.TicketBuffer.Count >= maxTickets || flow.Step != step)
                {
                    if (flow.Step == step)
                        flow.ProgressStep();
                    return;
                }
            }
        }

        private static Table GenerateTable(List<int> baseSet, List<int> scanSet, string keyTitle, string keyColumnBase, string keyColumnScan, string colorBase = "red", string colorScan = "green")
        {
            var table = new Table();
            table.Title(Localization.Get(keyTitle));
            table.AddColumn(Localization.Get(keyColumnBase));
            table.AddColumn(Localization.Get(keyColumnScan));
            scanSet.ForEach(ticket => baseSet.Remove(ticket));

            int maxIterations = Math.Max(baseSet.Count, scanSet.Count);

            for (int i = 0; i < maxIterations; i++)
            {
                var registeredTicket = i < baseSet.Count ? $"# [{colorBase}]{baseSet[i]}[/]" : "";
                var scannedTicket = i < scanSet.Count ? $"# [{colorScan}]{scanSet[i]}[/]" : "";

                table.AddRow(registeredTicket, scannedTicket);
            }

            return table;
        }

        private static void CloseMenu(string? message = null, bool closeMenu = true, bool subMenu = false, bool instant = false)
        {
            if (message != null)
                AnsiConsole.MarkupLine(message);

            if (!instant)
            {
                AnsiConsole.MarkupLine(Localization.Get("Guide_close_message"));
                Thread.Sleep(2000);
            }

            AnsiConsole.Clear();

            if (subMenu)
                ShowSubMenu = !closeMenu;
            else
                ShowMenu = !closeMenu;
            return;
        }
    }
}
