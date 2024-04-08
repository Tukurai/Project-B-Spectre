using Common.DAL;
using Common.DAL.Models;
using Common.Choices;
using Common.Services;
using Common.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Common.Enums;

namespace Management_Spectre
{
    internal class Program
    {
        public static bool Running { get; set; } = true;
        public static bool ShowMenu { get; set; } = true;
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
                var hasAccess = userService.ValidateUserForRole(userpass, Role.Manager);
                AnsiConsole.Clear(); // Clear the console after the ticket has been scanned

                if (hasAccess.Valid)
                    ShowMenu = true;

                while (ShowMenu)
                    ManagementMenu().NavigationAction();
            }

            Console.ReadLine();
        }

        public static NavigationChoice ManagementMenu()
        {
            var options = new List<NavigationChoice>() {
                new(Localization.Get("Management_close"), () => { CloseMenu(); }),
            };

            return Prompts.GetMenu("Management_title", "Management_menu_more_options", options);
        }

        public static NavigationChoice TourMenu(Tour tour)
        {
            var options = new List<NavigationChoice>() {
                new(Localization.Get("Management_close"), () => { CloseMenu(); }),
            };

            return Prompts.GetMenu("Management_title", "Management_menu_more_options", options);
        }


        private static void CloseMenu(string? message = null, bool closeMenu = true)
        {
            if (message != null)
                AnsiConsole.MarkupLine(message);

            AnsiConsole.MarkupLine(Localization.Get("Management_close_message"));
            Thread.Sleep(2000);

            AnsiConsole.Clear();
            ShowMenu = !closeMenu;
            return;
        }
    }
}
