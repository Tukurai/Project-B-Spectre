﻿using Common.DAL;
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
        public static User? User { get; set; }
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
                .AddScoped<RemoveTicketTourGuideFlow>()
                .AddScoped<AddTicketTourGuideFlow>()
                .AddScoped<StartTourGuideFlow>()
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
                AnsiConsole.Clear(); // Clear the console after the ticket has been scanned

                if (hasAccess.Valid)
                    ShowMenu = true;

                while (ShowMenu)
                    GuideMenu().NavigationAction();
            }

            Console.ReadLine();
        }

        public static NavigationChoice GuideMenu()
        {
            var tourService = ServiceProvider.GetService<TourService>()!;
            var settingsService = ServiceProvider.GetService<SettingsService>()!;

            var options = new List<NavigationChoice>() { };

            foreach (Tour tour in tourService.GetToursForToday(0, 2, 4))
            {
                var start = tour.Start.ToShortTimeString();
                var state = tour.Departed ? Localization.Get("Tour_departed") : Localization.Get("Tour_not_departed");
                var registered = $"({tour.RegisteredTickets.Count}/{settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value})";

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

            return Prompts.GetMenu("Guide_title", "Guide_menu_more_options", options);
        }

        public static NavigationChoice TourMenu(Tour tour)
        {
            var settingsService = ServiceProvider.GetService<SettingsService>()!;

            var ruleHeader = new Rule(Localization.Get("Guide_view_tour_title"));
            ruleHeader.Justification = Justify.Left;
            AnsiConsole.Write(ruleHeader);
            AnsiConsole.MarkupLine(Localization.Get("Guide_view_tour_description", replacementStrings: new() {
                tour.Start.ToShortTimeString(),
                tour.Departed ? Localization.Get("Tour_departed") : Localization.Get("Tour_not_departed"),
                $"({tour.RegisteredTickets.Count}/{settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value})"
            }));

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

            return Prompts.GetMenu("Guide_title", "Guide_menu_more_options", options);
        }

        private static void GuideRemoveTicket(Tour tour)
        {
            var flow = ServiceProvider.GetService<RemoveTicketTourGuideFlow>()!;

            // Set tour into flow
            var setTourResult = flow.SetTour(tour);
            if (!setTourResult.Success)
            {
                flow.Rollback();
                CloseMenu(setTourResult.Message, false);
                return;
            }

            var table = new Table();
            table.Title(Localization.Get("Remove_ticket_flow_title"));
            table.AddColumn(Localization.Get("Remove_ticket_flow_ticket_column"));
            flow.Tour!.RegisteredTickets.ForEach(ticket => table.AddRow($"# [green]{ticket}[/]"));
            AnsiConsole.Write(table);

            while (flow.Tour.RegisteredTickets.Any())
            {
                flow.RemoveTicket(Prompts.AskTicketNumber());
                AnsiConsole.Clear();

                table.Rows.Clear();
                table.AddColumn(Localization.Get("Remove_ticket_flow_ticket_column"));
                flow.Tour!.RegisteredTickets.ForEach(ticket => table.AddRow($"# [green]{ticket}[/]"));
                AnsiConsole.Write(table);

                if (flow.Tour.RegisteredTickets.Any() && Prompts.AskConfirmation("Remove_ticket_flow_ask_more_tickets"))
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

            var table = new Table();
            table.Title(Localization.Get("Add_ticket_flow_title"));
            table.AddColumn(Localization.Get("Add_ticket_flow_ticket_column"));
            flow.Tour!.RegisteredTickets.ForEach(ticket => table.AddRow($"# [green]{ticket}[/]"));
            AnsiConsole.Write(table);

            while (flow.Tour.RegisteredTickets.Count < settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value)
            {
                var ticketNumber = Prompts.AskTicketNumber();
                flow.AddTicket(ticketNumber);
                AnsiConsole.Clear();

                table.AddRow($"# [green]{ticketNumber}[/]");
                AnsiConsole.Write(table);

                if (flow.Tour.RegisteredTickets.Count < settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value 
                    && Prompts.AskConfirmation("Add_ticket_flow_ask_more_tickets"))
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

            AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets, flow.ScannedTickets));

            ScanTickets(flow, flow.Tour.RegisteredTickets.Count, FlowStep.ScanRegistration);

            AnsiConsole.MarkupLine(Localization.Get("Start_tour_flow_scan_extra"));

            ScanTickets(flow, settingsService.GetValueAsInt("Max_capacity_per_tour")!.Value, FlowStep.ScanRegistration);

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
            while (flow.ScannedTickets.Count < maxTickets)
            {
                var ticketNumber = Prompts.AskTicketNumber();
                if (ticketNumber.ToString().Length >= 8) // Guide & manager badges have an id with less then 8 digits. 
                    flow.AddScannedTicket(ticketNumber, step == FlowStep.ScanExtra);
                else
                    flow.ScanBadge(ticketNumber);
                AnsiConsole.Clear();

                AnsiConsole.Write(GenerateTable(flow.Tour!.RegisteredTickets, flow.ScannedTickets));

                if (flow.Tour.RegisteredTickets.Count >= flow.ScannedTickets.Count || flow.Step != step)
                    break;
            }
        }

        private static Table GenerateTable(List<int> registered, List<int> scanned)
        {
            var table = new Table();
            table.Title(Localization.Get("Start_tour_flow_title"));
            table.AddColumn(Localization.Get("Start_tour_flow_ticket_todo_column"));
            table.AddColumn(Localization.Get("Start_tour_flow_ticket_done_column"));
            scanned.ForEach(ticket => registered.Remove(ticket));

            int maxIterations = Math.Max(registered.Count, scanned.Count);

            for (int i = 0; i < maxIterations; i++)
            {
                var registeredTicket = i < registered.Count ? $"# [red]{registered[i]}[/]" : "";
                var scannedTicket = i < scanned.Count ? $"# [green]{scanned[i]}[/]" : "";

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
