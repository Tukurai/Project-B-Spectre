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
    public class ModifyReservationFlow : ReservationFlow
    {
        private GroupService GroupService { get; }
        public Tour? Tour { get; private set; }
        public Tour? NewTour { get; private set; }
        public Group? Group { get; private set; }

        public ModifyReservationFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService,
            TourService tourService, GroupService groupService)
            : base(context, localizationService, ticketService, tourService)
        {
            GroupService = groupService;
        }

        public override (bool Success, string Message) SetTicket(Ticket? ticket)
        {
            var baseResult = base.SetTicket(ticket);
            if (!baseResult.Success)
                return baseResult;

            Tour = TourService.GetTourForTicket(ticket!);

            if (Tour == null)
                return (false, Localization.Get("Flow_no_tour"));

            if (Tour.Departed)
                return (false, Localization.Get("Flow_tour_departed"));

            Group = GroupService.GetGroupForTicket(ticket!);
            if (Group == null)
                return (false, Localization.Get("Flow_no_group"));

            if (Group.GroupOwnerId != ticket!.Id)
                return (false, Localization.Get("Flow_not_group_owner"));

            return baseResult;
        }

        public (bool Success, string Message) SetTour(Tour tour)
        {
            if (Tour == null)
                return (false, Localization.Get("Flow_no_tour"));

            if (Tour.Departed)
                return (false, Localization.Get("Flow_tour_departed"));

            if (Tour.Id == tour.Id)
                return (false, Localization.Get("Flow_same_tour"));

            NewTour = tour;
            return (true, "Flow_new_tour_set");
        }

        public override (bool Succeeded, string Message) Commit()
        {
            if (Tour == null)
                return (false, Localization.Get("Flow_no_tour"));

            if (Group == null)
                return (false, Localization.Get("Flow_no_group"));

            if (NewTour == null)
                return (false, Localization.Get("Flow_no_new_tour"));

            Group!.GroupTickets.ForEach(groupTicket => Tour!.RegisteredTickets.Remove(groupTicket));
            NewTour!.RegisteredTickets.AddRange(Group.GroupTickets);

            return base.Commit();
        }
    }
}
