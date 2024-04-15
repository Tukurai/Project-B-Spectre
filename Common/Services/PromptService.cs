using Common.DAL;
using Common.DAL.Models;
using Common.Choices;
using Common.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{
    public class PromptService : BaseService
    {
        public SettingsService Settings { get; }
        public LocalizationService Localization { get; }
        public TicketService TicketService { get; }
        public TourService TourService { get; }
        public UserService UserService { get; }

        public PromptService(DepotContext context, SettingsService settings, LocalizationService localizationService,
            TicketService ticketService, TourService tourService, UserService userService)
            : base(context)
        {
            Localization = localizationService;
            TicketService = ticketService;
            UserService = userService;
            TourService = tourService;
            Settings = settings;
        }

        public int AskNumber(string questionKey, string validationErrorKey,int? min = null, int? max = null)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>(Localization.Get(questionKey))
                    .PromptStyle("green")
                    .ValidationErrorMessage(Localization.Get(validationErrorKey))
                    .Validate(inputNumber =>
                    {
                        if (min != null && inputNumber < min)
                            return ValidationResult.Error(Localization.Get("Input_below_minimum"));

                        if (max != null && inputNumber > max)
                            return ValidationResult.Error(Localization.Get("Input_exceeds_capacity"));

                        return ValidationResult.Success();
                    }));
        }

        public int AskTicketNumber()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>(Localization.Get("Scan_ticket"))
                    .PromptStyle("green")
                    .ValidationErrorMessage(Localization.Get("Invalid_ticket_number"))
                    .Validate(ticketNumberInput =>
                    {
                        var response = TicketService.ValidateTicketNumber(ticketNumberInput);

                        return response.Valid ? ValidationResult.Success()
                            : ValidationResult.Error(response.Message);
                    }));
        }

        public int AskTicketNumberOrUserpass()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>(Localization.Get("Scan_ticket_or_userpass"))
                    .PromptStyle("green")
                    .ValidationErrorMessage(Localization.Get("Invalid_ticket_number_or_userpass"))
                    .Validate(numberInput =>
                    {
                        var responseUser = UserService.ValidateUserpass(numberInput);
                        if (responseUser.Valid)
                            return ValidationResult.Success();

                        var responseTicket = TicketService.ValidateTicketNumber(numberInput);
                        if (responseTicket.Valid)
                            return ValidationResult.Success();

                        return ValidationResult.Error(responseTicket.Message);
                    }));
        }

        public int AskUserpass()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>(Localization.Get("Scan_userpass"))
                    .PromptStyle("green")
                    .ValidationErrorMessage(Localization.Get("Invalid_userpass"))
                    .Validate(userpassInput =>
                    {
                        var response = UserService.ValidateUserpass(userpassInput);

                        return response.Valid ? ValidationResult.Success()
                            : ValidationResult.Error(response.Message);
                    }));
        }

        public DateTime AskDate(string titleTranslationKey, string moreOptionsTranslationKey, int dateRange = 31)
        {
            var start = DateTime.Today.Date;
            var dateChoices = Enumerable.Range(0, dateRange).Select(offset => new DateChoice(start.AddDays(offset)));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<DateChoice>()
                    .Title(Localization.Get(titleTranslationKey))
                    .PageSize(10)
                    .MoreChoicesText(Localization.Get(moreOptionsTranslationKey))
                    .AddChoices(dateChoices));

            return choice.Date;
        }


        public TimeSpan AskTime(string titleTranslationKey, string moreOptionsTranslationKey, int timeInterval = 30)
        {
            var minutes = 0;
            var timeChoices = new List<TimeChoice>();
            while (minutes < 1440)
            {
                minutes = Math.Min(1440, minutes);
                timeChoices.Add(new TimeChoice(new TimeSpan(0, minutes, 0)));
                minutes += timeInterval;
            }

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<TimeChoice>()
                    .Title(Localization.Get(titleTranslationKey))
                    .PageSize(10)
                    .MoreChoicesText(Localization.Get(moreOptionsTranslationKey))
                    .AddChoices(timeChoices));

            return choice.Span;
        }

        public NavigationChoice GetMenu(string titleTranslationKey, string moreOptionsTranslationKey, List<NavigationChoice> navigationChoices)
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<NavigationChoice>()
                    .Title(Localization.Get(titleTranslationKey))
                    .PageSize(10)
                    .MoreChoicesText(Localization.Get(moreOptionsTranslationKey))
                    .AddChoices(navigationChoices));
        }

        public Tour AskTour(string titleTranslationKey, string moreOptionsTranslationKey, int minimumCapacity, int recentTours = 0, int upcomingTours = -1)
        {
            int maxCapacity = Settings.GetValueAsInt("Max_capacity_per_tour")!.Value;

            var tourChoices = TourService.GetToursForToday(minimumCapacity, recentTours, upcomingTours)
                .Select(tour => new TourChoice($"{tour.Start.ToShortTimeString()}, ({tour.RegisteredTickets.Count}/{maxCapacity})", tour));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<TourChoice>()
                    .Title(Localization.Get(titleTranslationKey))
                    .PageSize(10)
                    .MoreChoicesText(Localization.Get(moreOptionsTranslationKey))
                    .AddChoices(tourChoices));

            return choice.Tour;
        }

        public bool AskConfirmation(string titleTranslationKey)
        {
             var choice = AnsiConsole.Prompt(
                new SelectionPrompt<BoolChoice>()
                    .Title(Localization.Get(titleTranslationKey))
                    .PageSize(10)
                    .AddChoices(new List<BoolChoice>() { 
                        new(Localization.Get("Choice_yes"), true),
                        new(Localization.Get("Choice_no"), false)
                    }));
            return choice.Choice;
        }
    }
}
