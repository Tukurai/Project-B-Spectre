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
        private SettingsService SettingsService { get; }

        public AddTicketTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService, SettingsService settingsService) 
            : base(context, localizationService, ticketService, tourService)
        {
            SettingsService = settingsService;
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
    }
}
