using Common.DAL;
using Common.DAL.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Workflows
{
    public class TourGuideFlow : Workflow
    {
        public TourService TourService { get; set; }
        public Tour? Tour { get; private set; }

        public TourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService) 
            : base(context, localizationService, ticketService)
        {
            TourService = tourService;
        }

        public virtual (bool Success, string Message) SetTour(Tour? tour)
        {
            if (tour == null)
                return (false, Localization.Get("Flow_tour_not_found"));

            if (tour.Departed)
                return (false, Localization.Get("Flow_cannot_edit_tour_departed"));

            Tour = tour;

            return (true, Localization.Get("flow_tour_is_valid"));
        }
    }
}
