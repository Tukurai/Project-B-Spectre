using Common.DAL;
using Common.DAL.Models;
using Common.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common.Workflows
{
    public class AddTicketTourGuideFlow : TourGuideFlow
    {
        public List<int> TicketsToAdd { get; private set; } = new List<int>();
        private SettingsService SettingsService { get; }

        public AddTicketTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService, SettingsService settingsService) 
            : base(context, localizationService, ticketService, tourService)
        {
            SettingsService = settingsService;
        }

        public (bool Success, string Message) AddTicket(int ticketNumber)
        {
            var validation = ValidateTicket(ticketNumber);
            if (!validation.Success)
                return validation;

            if (Tour!.RegisteredTickets.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_already_in_tour"));

            if (TourService.GetTourForTicket(ticketNumber) != null)
                return (false, Localization.Get("Flow_ticket_already_in_other_tour"));

            if (TicketsToAdd.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_already_added_to_list"));

            TicketsToAdd.Add(ticketNumber);

            return (true, Localization.Get("Flow_ticket_added_to_list"));
        }

        public override (bool Success, string Message) SetTour(Tour? tour)
        {
            var baseResult = base.SetTour(tour);
            if (!baseResult.Success)
                return baseResult;

            int maxCapacity = SettingsService.GetValueAsInt("Max_capacity_per_tour")!.Value;

            if (tour!.RegisteredTickets.Count >= maxCapacity)
                return (false, Localization.Get("Flow_tour_no_space_for_tickets_in_tour"));

            return (true, Localization.Get("flow_tour_is_valid"));
        }

        public override (bool Succeeded, string Message) Commit()
        {
            if (!TicketsToAdd.Any())
                return (false, Localization.Get("Flow_no_tickets_to_add"));

            TicketsToAdd.ForEach(t => Tour!.RegisteredTickets.Add(t));

            return base.Commit();
        }
    }
}
