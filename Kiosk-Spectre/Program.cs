using Common.DAL;
using Common.DAL.Models;
using Common.Choices;
using Common.Services;
using Common.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Kiosk_Spectre
{
    internal class Program
    {
        public static bool Running { get; set; } = true;
        public static bool ShowMenu { get; set; } = true;
        public static Ticket? Ticket { get; set; }
        public static ServiceProvider ServiceProvider { get; set; }
        public static LocalizationService Localization { get; set; }
        public static TourService TourService { get; set; }
        public static PromptService Prompts { get; set; }
        public static SettingsService SettingsService { get; set; }

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
                .AddTransient<CancelReservationFlow>()
                .AddTransient<CreateReservationFlow>()
                .AddTransient<ModifyReservationFlow>()
                .BuildServiceProvider();

            // Get services
            Localization = ServiceProvider.GetService<LocalizationService>()!;
            Prompts = ServiceProvider.GetService<PromptService>()!;
            TourService = ServiceProvider.GetService<TourService>()!;
            SettingsService = ServiceProvider.GetService<SettingsService>()!;
            var TicketService = ServiceProvider.GetService<TicketService>()!;

            // Setup context
            ServiceProvider.GetService<DepotContext>()!.LoadContext();

            // Menu loop
            while (Running)
            {
                var ticketNumber = Prompts.AskTicketNumber();

                Ticket = TicketService.GetTicket(ticketNumber)!; // Ticket can't be null here due to validation
                AnsiConsole.Clear(); // Clear the console after the ticket has been scanned

                while (ShowMenu)
                {
                    var tour = TourService.GetTourForTicket(Ticket.Id);

                    (tour == null ? ReservationMenu() : ModificationMenu()).NavigationAction();
                }
                ShowMenu = true;
            }

            Console.ReadLine();
        }

        public static NavigationChoice ReservationMenu()
        {
            var options = new List<NavigationChoice>() {
                new(Localization.Get("Kiosk_reservation"), TourReservation),
                new(Localization.Get("Kiosk_close"), () => { CloseMenu(); }),
            };

            // Menu for reservation
            return Prompts.GetMenu("Kiosk_title", "Kiosk_menu_more_options", options);
        }

        public static NavigationChoice ModificationMenu()
        {
            var options = new List<NavigationChoice>() {
                new(Localization.Get("Kiosk_modification"), TourModification),
                new(Localization.Get("Kiosk_cancellation"), TourCancellation),
                new(Localization.Get("Kiosk_close"), () => { CloseMenu(); }),
            };

            // Menu for modification of a reservation
            return Prompts.GetMenu("Kiosk_title", "Kiosk_menu_more_options", options);
        }

        private static void CloseMenu(string? message = null, bool closeMenu = true)
        {
            if (message != null)
                AnsiConsole.MarkupLine(message);

            AnsiConsole.MarkupLine(Localization.Get("Kiosk_close_message"));
            Thread.Sleep(2000);

            AnsiConsole.Clear();
            ShowMenu = !closeMenu;
            return;
        }

        public static void TourReservation()
        {
            var tourService = ServiceProvider.GetService<TourService>();
            var flow = ServiceProvider.GetService<CreateReservationFlow>()!;

            // Set ticket into flow
            flow.SetTicket(Ticket);

            // Choose a tour
            if (!TourService.GetToursForToday(flow.GroupTickets.Count, 0, -1).Any())
            {
                CloseMenu(Localization.Get("Flow_no_tours"), false);
                return;
            }

            int maxCapacity = SettingsService.GetValueAsInt("Max_capacity_per_tour")!.Value;

            var table = new Table();
            table.Title(Localization.Get("Reservation_flow_title"));
            table.AddColumn(Localization.Get("Reservation_flow_ticket_column"));
            table.AddRow($"# [green]{flow.GroupTickets.Last().Id}[/]");
            AnsiConsole.Write(table);

            // Ask for the amount of people to make a reservation for
            var ticketAmount = Prompts.AskNumber("Flow_reservation_people_amount", "Flow_reservation_Invalid_people_amount", 1, maxCapacity);

            if (!TourService.GetToursForToday(ticketAmount).Any())
            {
                CloseMenu(Localization.Get("Flow_no_tours"), false);
                return;
            }

            // Ask for additional tickets if there are more than 1 people in this group
            while (flow.GroupTickets.Count() < ticketAmount)
            {
                var addTicketResult = flow.AddTicket(Prompts.AskTicketNumber());
                if (!addTicketResult.Success)
                {
                    AnsiConsole.MarkupLine(addTicketResult.Message);
                    continue;
                }

                table.AddRow($"# [green]{flow.GroupTickets.Last().Id}[/]");
                AnsiConsole.Clear();

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"{Localization.Get("Flow_reservation_people_amount")} [green]{ticketAmount}[/]");
            }

            var tour = Prompts.AskTour("Reservation_flow_ask_tour", "Reservation_flow_more_tours", ticketAmount);
            flow.SetTour(tour);
            AnsiConsole.MarkupLine(Localization.Get("Reservation_flow_selected_tour", replacementStrings: new() { $"{tour.Start.ToString("HH:mm")}" }));

            // Commit the flow.
            if (Prompts.AskConfirmation("Reservation_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message);
                return;
            }
            CloseMenu();
        }

        public static void TourModification()
        {
            var flow = ServiceProvider.GetService<ModifyReservationFlow>()!;
            AnsiConsole.MarkupLine(Localization.Get("Modification_flow_title"));

            var setTicketResult = flow.SetTicket(Ticket);
            if (!setTicketResult.Success)
            {
                CloseMenu(setTicketResult.Message, false);
                return;
            }

            AnsiConsole.MarkupLine(Localization.Get("Modification_flow_selected_tour", replacementStrings: new() { $"{flow.Tour!.Start.ToString("HH:mm")}" }));

            if (!Prompts.AskConfirmation("Modification_flow_ask_confirmation"))
            {
                CloseMenu(Localization.Get("Modification_flow_not_changed"), false);
                return;
            }

            if (!TourService.GetToursForToday(flow.Group!.GroupTickets.Count).Any())
            {
                CloseMenu(Localization.Get("Flow_no_tours"), false);
                return;
            }

            // Choose a tour
            var tour = Prompts.AskTour("Modification_flow_ask_tour", "Modification_flow_more_tours", flow.Group!.GroupTickets.Count);
            
            var setTourResult = flow.SetTour(tour);
            if (!setTourResult.Success)
            {
                CloseMenu(setTourResult.Message, false);
                return;
            }

            AnsiConsole.MarkupLine(Localization.Get("Modification_flow_selected_new_tour", replacementStrings: new() { $"{tour.Start.ToString("HH:mm")}" }));

            // Commit the flow.
            if (Prompts.AskConfirmation("Modification_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message, false);
                return;
            }
            CloseMenu();
        }

        public static void TourCancellation()
        {
            var flow = ServiceProvider.GetService<CancelReservationFlow>()!;
            AnsiConsole.MarkupLine(Localization.Get("Cancellation_flow_title"));

            var setTicketResult = flow.SetTicket(Ticket);
            if (!setTicketResult.Success)
            {
                CloseMenu(setTicketResult.Message, false);
                return;
            }

            AnsiConsole.MarkupLine(Localization.Get("Cancellation_flow_selected_tour", replacementStrings: new() { $"{flow.Tour!.Start.ToString("HH:mm")}" }));

            if (Prompts.AskConfirmation("Cancellation_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message);
                return;
            }
            CloseMenu();
        }
    }
}
