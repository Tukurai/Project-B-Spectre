using Common.DAL;
using Common.DAL.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common.Workflows
{
    public class RemoveTicketTourGuideFlow: TourGuideFlow
    {

        public RemoveTicketTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService) 
            : base(context, localizationService, ticketService, tourService)
        {
        }

        public (bool Success, string Message) RemoveTicket(int ticketNumber)
        {
            var validation = ValidateTicket(ticketNumber);
            if (!validation.Success)
                return validation;

            if(!Tour!.RegisteredTickets.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_not_in_tour"));

            if (TicketBuffer.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_already_added_to_list"));

            TicketBuffer.Add(ticketNumber);

            return (true, Localization.Get("Flow_ticket_added_to_list"));
        }

        public override (bool Success, string Message) SetTour(Tour? tour)
        {
            var baseResult = base.SetTour(tour);
            if (!baseResult.Success)
                return baseResult;

            if (!tour!.RegisteredTickets.Any())
                return (false, Localization.Get("Flow_tour_no_tickets_in_tour"));

            return (true, Localization.Get("flow_tour_is_valid"));
        }

        public override (bool Succeeded, string Message) Commit()
        {
            if (!TicketBuffer.Any())
                return (false, Localization.Get("Flow_no_tickets_to_remove"));

            // TODO: Take groups into account.
            TicketBuffer.ForEach(t => Tour!.RegisteredTickets.Remove(t));

            return base.Commit();
        }

        public override (bool Succeeded, string Message) Rollback()
        {
            TicketBuffer.Clear();

            return base.Rollback();
        }
    }
}
