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
    public class StartTourGuideFlow : TourGuideFlow
    {
        private SettingsService SettingsService { get; }

        public StartTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService, SettingsService settingsService) 
            : base(context, localizationService, ticketService, tourService)
        {
            SettingsService = settingsService;
        }
    }
}
