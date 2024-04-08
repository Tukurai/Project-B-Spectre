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
                .AddScoped<CancelReservationFlow>()
                .AddScoped<CreateReservationFlow>()
                .AddScoped<ModifyReservationFlow>()
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
            CloseMenu(closeMenu:false, subMenu: true);
        }

        private static void GuideAddTicket(Tour tour)
        {
            CloseMenu(closeMenu: false, subMenu: true);
        }

        private static void GuideStartTour(Tour tour)
        {
            CloseMenu(closeMenu: false, subMenu: true);
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
