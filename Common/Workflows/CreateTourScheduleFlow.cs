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
    public class CreateTourScheduleFlow : Workflow
    {
        private SettingsService SettingsService { get; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public int Interval { get; private set; }
        private Dictionary<DateTime, List<TimeSpan>> Planning { get; set; } = new Dictionary<DateTime, List<TimeSpan>>();
        private List<Tour> ToursToDispose { get; set; } = new List<Tour>();
        private bool Regenerate { get; set; } = true;

        public CreateTourScheduleFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, SettingsService settingsService)
            : base(context, localizationService, ticketService)
        {
            SettingsService = settingsService;
        }

        public (bool Success, string Message) SetDateSpan(DateTime? start, DateTime? end)
        {
            if (start == null)
                return (false, Localization.Get("Flow_date_start_null"));

            if (start.Value.Date < DateTime.Now.Date)
                return (false, Localization.Get("Flow_date_start_in_past"));

            if (end != null && end.Value < start.Value)
                return (false, Localization.Get("Flow_date_end_before_start"));

            StartDate = start.Value;
            EndDate = end ?? start.Value;
            Regenerate = true;

            return (true, Localization.Get("Flow_date_span_set"));
        }

        public (bool Success, string Message) SetTimeSpan(TimeSpan startTime, TimeSpan endTime)
        {
            if (endTime < startTime)
                return (false, Localization.Get("Flow_time_end_before_start"));

            StartTime = startTime;
            EndTime = endTime;
            Regenerate = true;

            return (true, Localization.Get("Flow_time_span_set"));
        }

        public (bool Success, string Message) SetInterval(int interval)
        {
            if (interval <= 0)
                return (false, Localization.Get("Flow_interval_invalid"));

            if (interval > 60)
                return (false, Localization.Get("Flow_interval_too_large"));

            Interval = interval;
            Regenerate = true;

            return (true, Localization.Get("Flow_interval_set"));
        }

        public Dictionary<DateTime, List<TimeSpan>> GetPreviewChanges()
        {
            if (Planning.Any() && !Regenerate)
                return Planning;

            for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                var times = new List<TimeSpan>();

                for (var time = StartTime; time.Add(TimeSpan.FromMinutes(SettingsService.GetValueAsInt("Tour_duration")!.Value)) < EndTime; time = time.Add(TimeSpan.FromMinutes(Interval)))
                    times.Add(time);

                Planning.Add(date, times);
            }

            Regenerate = false;
            return Planning;
        }

        public (bool Succeeded, string Message) DisposePlanning(Dictionary<DateTime, List<Tour>> currentPlanning)
        {
            foreach (var (_, tours) in currentPlanning)
                ToursToDispose.AddRange(tours.Where(q => !q.Departed));

            return (true, Localization.Get("Flow_planning_marked_for_disposal"));
        }

        public override (bool Succeeded, string Message) Commit()
        {
            var planning = new List<Tour>();
            if (Planning.Any())
                foreach (var (date, times) in Planning)
                    foreach (var time in times)
                        planning.Add(new Tour() { Start = date.Add(time), RegisteredTickets = new List<int>() });
            else
                return (false, Localization.Get("Flow_no_planning_to_commit"));
            
            if(ToursToDispose.Any())
                Context.Tours.RemoveRange(ToursToDispose);

            Context.Tours.AddRange(planning);

            return base.Commit();
        }
    }
}
