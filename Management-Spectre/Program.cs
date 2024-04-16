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
                .AddSingleton<UserService>()
                .AddTransient<CreateUserFlow>()
                .AddTransient<CreateTourScheduleFlow>()
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
                new(Localization.Get("Management_plan_tours_today"), () => { PlanTour(DateTime.Today); }),
                new(Localization.Get("Management_plan_tours_tomorrow"), () => { PlanTour(DateTime.Today.AddDays(1)); }),
                new(Localization.Get("Management_plan_tours_in_future"), () => { PlanTour(); }),
                new(Localization.Get("Management_view_tours"), ViewTours),
                new(Localization.Get("Management_user_creation"), CreateUser),
                new(Localization.Get("Management_view_users"), ViewUsers),
                new(Localization.Get("Management_close"), () => { CloseMenu(); }),
            };

            return Prompts.GetMenu("Management_title", "Management_menu_more_options", options);
        }

        private static void ViewUsers()
        {
            var userService = ServiceProvider.GetService<UserService>()!;

            var currentUsers = userService.GetAllUsers();

            var currentPlanningTable = new Table();
            currentPlanningTable.AddColumn(Localization.Get("View_user_id_column"));
            currentPlanningTable.AddColumn(Localization.Get("View_user_role_column"));
            currentPlanningTable.AddColumn(Localization.Get("View_user_name_column"));
            currentPlanningTable.AddColumn(Localization.Get("View_user_enabled_column"));

            foreach (var user in currentUsers)
            {
                var id = $"[grey]{user.Id}[/]";
                var role = $"[blue]{(Role)user.Role}[/]";
                var name = $"[green]{user.Name}[/]";
                var enabled = user.Enabled ? "[green]enabled[/]" : "[red]disabled[/]";

                currentPlanningTable.AddRow(id, role, name, enabled);
            }

            var currentPlanningHeader = new Rule(Localization.Get("View_user_current_users"));
            currentPlanningHeader.Justification = Justify.Left;
            AnsiConsole.Write(currentPlanningHeader);
            AnsiConsole.Write(currentPlanningTable);

            AnsiConsole.WriteLine(Localization.Get("View_user_press_any_key_to_continue"));

            Console.ReadKey();

            CloseMenu(closeMenu: false);
            return;
        }

        private static void CreateUser()
        {
            var flow = ServiceProvider.GetService<CreateUserFlow>()!;

            flow.SetUsername(Prompts.AskUsername());
            flow.SetRole(Prompts.AskRole());

            // Commit the flow.
            if (Prompts.AskConfirmation("Create_user_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message, false);
                return;
            }

            flow.Rollback();
            CloseMenu(closeMenu: false);
        }

        private static void ViewTours()
        {
            var tourService = ServiceProvider.GetService<TourService>()!;

            var start = Prompts.AskDate("View_tour_start_date", "View_tour_more_dates");
            var end = Prompts.AskDate("Create_tour_flow_end_date", "Create_tour_flow_more_dates", startDate: start);

            var currentPlanning = tourService.GetToursForTimespan(start, end);

            var currentPlanningTable = new Table();
            currentPlanningTable.AddColumn(Localization.Get("View_tour_date_column"));
            currentPlanningTable.AddColumn(Localization.Get("View_tour_time_column"));
            
            foreach (var (date, tours) in currentPlanning)
                currentPlanningTable.AddRow($"[green]{date.ToString("dd/MM/yyyy")}[/]", string.Join(", ", tours.Select(tour => $"[blue]{tour.Start.ToString("hh\\:mm")}[/]")));

            var currentPlanningHeader = new Rule(Localization.Get("View_tour_current_planning"));
            currentPlanningHeader.Justification = Justify.Left;
            AnsiConsole.Write(currentPlanningHeader);
            AnsiConsole.Write(currentPlanningTable);

            AnsiConsole.WriteLine(Localization.Get("View_tour_press_any_key_to_continue"));

            Console.ReadKey();

            CloseMenu(closeMenu: false);
            return;
        }

        private static void PlanTour(DateTime? start = null, DateTime? end = null)
        {
            var tourService = ServiceProvider.GetService<TourService>()!;
            var flow = ServiceProvider.GetService<CreateTourScheduleFlow>()!;

            if (!flow.SetDateSpan(start, end).Success)
            {
                start = Prompts.AskDate("Create_tour_flow_start_date", "Create_tour_flow_more_dates");
                end = Prompts.AskDate("Create_tour_flow_end_date", "Create_tour_flow_more_dates", startDate: start);

                flow.SetDateSpan(start, end);
            }

            var startTime = Prompts.AskTime("Create_tour_flow_start_time", "Create_tour_flow_more_times");
            var endTime = Prompts.AskTime("Create_tour_flow_end_time", "Create_tour_flow_more_times", startTime: startTime.Minutes);

            flow.SetTimeSpan(startTime, endTime);

            var interval = Prompts.AskNumber("Create_tour_flow_interval", "Create_tour_flow_interval_invalid", 1, 60);

            flow.SetInterval(interval);

            var previewChanges = flow.GetPreviewChanges();

            var newPlanning = new Table();
            newPlanning.AddColumn(Localization.Get("Create_tour_flow_date_column"));
            newPlanning.AddColumn(Localization.Get("Create_tour_flow_time_column"));

            foreach (var (date, times) in previewChanges)
                newPlanning.AddRow($"[green]{date.ToString("dd/MM/yyyy")}[/]", string.Join(", ", times.Select(time => $"[blue]{time.ToString("hh\\:mm")}[/]")));

            var newPlanningHeader = new Rule(Localization.Get("Create_tour_flow_new_planning"));
            newPlanningHeader.Justification = Justify.Left;
            AnsiConsole.Write(newPlanningHeader);
            AnsiConsole.Write(newPlanning);

            var currentPlanning = tourService.GetToursForTimespan(flow.StartDate, flow.EndDate);

            var oldPlanning = new Table();
            oldPlanning.AddColumn(Localization.Get("Create_tour_flow_date_column"));
            oldPlanning.AddColumn(Localization.Get("Create_tour_flow_time_column"));

            foreach (var (date, tours) in currentPlanning)
                oldPlanning.AddRow($"[green]{date.ToString("dd/MM/yyyy")}[/]", string.Join(", ", tours.Select(tour => $"[blue]{tour.Start.ToString("hh\\:mm")}[/]")));

            var oldPlanningHeader = new Rule(Localization.Get("Create_tour_flow_old_planning"));
            oldPlanningHeader.Justification = Justify.Left;
            AnsiConsole.Write(oldPlanningHeader);
            AnsiConsole.Write(oldPlanning);

            if (currentPlanning.Any() && Prompts.AskConfirmation("Create_tour_flow_overwrite_current_confirmation"))
                flow.DisposePlanning(currentPlanning);

            // Commit the flow.
            if (Prompts.AskConfirmation("Create_tour_flow_ask_confirmation"))
            {
                var commitResult = flow.Commit();
                CloseMenu(commitResult.Message, false);
                return;
            }

            flow.Rollback();
            CloseMenu(closeMenu: false);
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
