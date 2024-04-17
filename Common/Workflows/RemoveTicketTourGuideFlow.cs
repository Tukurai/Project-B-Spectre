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
    public class RemoveTicketTourGuideFlow : TourGuideFlow
    {
        private GroupService GroupService { get; }

        public RemoveTicketTourGuideFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService, GroupService groupService)
            : base(context, localizationService, ticketService, tourService)
        {
            GroupService = groupService;
        }

        public (bool Success, string Message) RemoveTicket(int ticketNumber, bool deleteGroup)
        {
            var validation = ValidateTicket(ticketNumber);
            if (!validation.Success)
                return validation;

            if (!Tour!.RegisteredTickets.Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_not_in_tour"));

            if (TicketBuffer.Keys.ToList().Contains(ticketNumber))
                return (false, Localization.Get("Flow_ticket_already_added_to_list"));

            var group = GroupService.GetGroupForTicket(ticketNumber)!;
            if (group.GroupTickets.Count > 1 && deleteGroup)
                foreach (int ticket in group.GroupTickets)
                    TicketBuffer.Add(ticket, deleteGroup);
            else
                TicketBuffer.Add(ticketNumber, deleteGroup);

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

            foreach ((int ticketNumber, bool deleteGroup) in TicketBuffer)
            {
                var group = GroupService.GetGroupForTicket(ticketNumber)!;

                if (!Tour!.RegisteredTickets.Contains(ticketNumber))
                    continue; // Was added as a way to display the impact of deleting a group

                if (group.GroupOwnerId == ticketNumber) // I am the group owner
                {
                    if (deleteGroup) // Delete my group
                    {
                        group.GroupTickets.ForEach(ticket => Tour!.RegisteredTickets.Remove(ticket));
                        GroupService.DeleteGroup(group);
                    }
                    else // Delete my group, but keep them in the tour as individuals
                    {
                        Tour!.RegisteredTickets.Remove(ticketNumber);

                        group.GroupTickets.Remove(ticketNumber);
                        foreach (int ticket in group.GroupTickets)
                        {
                            GroupService.AddGroup(new Group() { GroupOwnerId = ticket, GroupTickets = new() { ticket } });
                        }
                        GroupService.DeleteGroup(group);
                    }
                }
                else // I am in a group, remove me from the group and tour
                {
                    group.GroupTickets.Remove(ticketNumber);
                    Tour!.RegisteredTickets.Remove(ticketNumber);
                }
            }

            return base.Commit();
        }

        public override (bool Succeeded, string Message) Rollback()
        {
            TicketBuffer.Clear();

            return base.Rollback();
        }
    }
}
