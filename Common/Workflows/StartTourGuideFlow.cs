using Common.DAL;
using Common.DAL.Models;
using Common.Enums;
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
        private UserService UserService { get; }
        public List<int> ScannedTickets { get; private set; } = new List<int>();
        public FlowStep Step { get; set; } = FlowStep.ScanRegistration;
        public int GuideId { get; private set; }

        public StartTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService, SettingsService settingsService, UserService userService) 
            : base(context, localizationService, ticketService, tourService)
        {
            SettingsService = settingsService;
            UserService = userService;
        }

        public (bool Success, string Message) AddScannedTicket(int ticketNumber, bool extra = false)
        {
            var validation = ValidateTicket(ticketNumber);
            if (!validation.Success)
                return validation;

            if (!Tour!.RegisteredTickets.Contains(ticketNumber) && !extra)
                return (false, Localization.Get("Flow_ticket_not_in_tour"));

            if (ScannedTickets.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_already_added_to_list"));

            if (ScannedTickets.Count >= SettingsService.GetValueAsInt("Max_capacity_per_tour")!.Value)
                return (false, Localization.Get("Flow_tour_no_space_for_tickets_in_tour"));

            return (true, Localization.Get("Flow_ticket_added_to_list"));
        }

        public (bool Success, string Message) ScanBadge(int userId)
        {
            var validation = UserService.ValidateUserpass(userId);
            if (!validation.Valid)
                return validation;

            GuideId = userId;

            switch(Step)
            {
                case FlowStep.ScanRegistration:
                    Step = FlowStep.ScanExtra;
                    break;
                case FlowStep.ScanExtra:
                    Step = FlowStep.Finalize;
                    break;
            }

            return (true, Localization.Get("Flow_next_step"));
        }

        public override (bool Succeeded, string Message) Commit()
        {
            if (!ScannedTickets.Any())
                return (false, Localization.Get("Flow_no_tickets_scanned"));

            Tour!.RegisteredTickets = ScannedTickets;
            Tour!.GuideId = GuideId;
            Tour!.Departed = true;

            return base.Commit();
        }
    }
}
