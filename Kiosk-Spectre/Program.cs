using Common.DAL;
using Common.Navigation;
using Common.Static;
using Common.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Kiosk_Spectre
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<DepotContext>()
                .AddSingleton<LocalizationService>()
                .AddSingleton<ConfigService>()
                .AddScoped<Workflow>()
                .BuildServiceProvider();

            var LocalizationService = serviceProvider.GetService<LocalizationService>();
            var ConfigService = serviceProvider.GetService<ConfigService>();

            var navigationChoice = AnsiConsole.Prompt(
            new SelectionPrompt<NavigationChoice>()
                .Title(LocalizationService!.Get("Kiosk_title")) // "Kiosk"
                .PageSize(10)
                .MoreChoicesText(LocalizationService!.Get("Kiosk_menu_more_options")) // "[grey](Move up and down to reveal more options)[/]"
                .AddChoices(new List<NavigationChoice>() {
                    new("Apple", DoApple), 
                    new ("Apricot", DoApricot),
                }));

            navigationChoice.NavigationAction();

            Console.ReadLine();
        }

        public static void DoApple()
        {
            AnsiConsole.MarkupLine("You chose [green]apple[/]!");
        }

        public static void DoApricot()
        {
            AnsiConsole.MarkupLine("You chose [green]Apricot[/]!");
        }
    }
}
