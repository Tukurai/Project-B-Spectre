using Common.DAL;
using Common.DAL.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Common.Workflows
{
    public class CreateReservationFlow : ReservationFlow
    {
        public List<Ticket> GroupTickets { get; } = new List<Ticket>();
        public Tour? Tour { get; private set; }

        public CreateReservationFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, TourService tourService)
            : base(context, localizationService, ticketService, tourService)
        {
        }

        public override (bool Success, string Message) SetTicket(Ticket? ticket)
        {
            var baseResult = base.SetTicket(ticket);
            if (!baseResult.Success)
                return baseResult;

            GroupTickets.Add(ticket!);

            return baseResult;
        }

        public (bool Success, string Message) AddTicket(int ticketNumber)
        {
            var ticket = TicketService.GetTicket(ticketNumber);

            var validationResult = ValidateTicket(ticket);
            if (!validationResult.Success)
                return validationResult;

            if (GroupTickets.Contains(ticket!))
                return (false, Localization.Get("Flow_ticket_already_added"));

            if (TourService.GetTourForTicket(ticketNumber) != null)
                return (false, Localization.Get("Flow_ticket_already_in_other_tour"));

            GroupTickets.Add(ticket!);

            return validationResult;

        }

        public void SetTour(Tour tour)
        {
            Tour = tour;
        }

        public override (bool Succeeded, string Message) Commit()
        {
            var group = new Group() { 
                GroupOwnerId = GroupTickets.First().Id, 
                GroupTickets = GroupTickets.Select(t => t.Id).ToList() 
            };

            Context.Groups.Add(group);
            Tour!.RegisteredTickets.AddRange(group.GroupTickets);

            return base.Commit();
        }
    }
}
