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

        public override (bool Success, string Message) SetTour(Tour? tour)
        {
            var baseResult = base.SetTour(tour);
            if (!baseResult.Success)
                return baseResult;

            if (!tour!.RegisteredTickets.Any())
                return (false, Localization.Get("Flow_tour_no_tickets_in_tour"));

            return (true, Localization.Get("flow_tour_is_valid"));
        }
    }
}
