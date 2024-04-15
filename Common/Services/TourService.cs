using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;
using Common.DAL.Models;

namespace Common.Services
{
    public class TourService : BaseService
    {
        public SettingsService Settings { get; }
        private LocalizationService Localization { get; }

        public TourService(DepotContext context, SettingsService settings, LocalizationService localization)
            : base(context)
        {
            Localization = localization;
            Settings = settings;
        }

        public Tour? GetTourForTicket(Ticket ticket) => GetTourForTicket(ticket.Id);

        public Tour? GetTourForTicket(int ticketNumber)
        {
            return Context.Tours.FirstOrDefault(tour => tour.RegisteredTickets.Contains(ticketNumber));
        }

        public List<Tour> GetToursForToday(int minimumCapacity = 0, int recentTours = -1, int upcomingTours = -1)
        {
            int maxCapacity = Settings.GetValueAsInt("Max_capacity_per_tour")!.Value;
            var tours = new List<Tour>(); // minimumCapacity is ignored for recent tours.

            if (recentTours > 0) // Add the most recent tours to the list but limit to the amount of recentTours
                tours.AddRange(Context.Tours.Where(tour => tour.Start < DateTime.Now && tour.Start.Date == DateTime.Today)
                    .OrderByDescending(tour => tour.Start).Take(recentTours).Reverse());
            else if (recentTours == -1) // show all recent tours
                tours.AddRange(Context.Tours.Where(tour => tour.Start < DateTime.Now && tour.Start.Date == DateTime.Today)
                    .OrderBy(tour => tour.Start));

            if (upcomingTours > 0) // restrict the amount of tours to be shown to the amount of upcomingTours
                tours.AddRange(Context.Tours.Where(tour => tour.Start > DateTime.Now && tour.Start.Date == DateTime.Today)
                    .Where(tour => (maxCapacity - tour.RegisteredTickets.Count) >= minimumCapacity)
                    .OrderBy(tour => tour.Start).Take(upcomingTours));
            else if (upcomingTours == -1) // show all upcoming tours
                tours.AddRange(Context.Tours.Where(tour => tour.Start > DateTime.Now && tour.Start.Date == DateTime.Today)
                    .Where(tour => (maxCapacity - tour.RegisteredTickets.Count) >= minimumCapacity)
                    .OrderBy(tour => tour.Start));

            return tours;
        }

        public Dictionary<DateTime, List<Tour>> GetToursForTimespan(DateTime start, DateTime end)
        {
            var tours = Context.Tours.Where(tour => tour.Start.Date >= start.Date && tour.Start.Date <= end.Date)
                .OrderBy(tour => tour.Start).ToList();
            return tours.GroupBy(q => q.Start.Date).ToDictionary(q => q.Key, q => q.ToList());
        }
    }
}
