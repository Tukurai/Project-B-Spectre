using Common.DAL;
using Common.DAL.Models;

namespace Common.Services
{
    public class GroupService : BaseService
    {
        public SettingsService Settings { get; }
        private LocalizationService Localization { get; }

        public GroupService(DepotContext context, SettingsService settings, LocalizationService localization) 
            : base(context)
        {
            Localization = localization;
            Settings = settings;
        }

        public Group? GetGroupForTicket(Ticket ticket) => GetGroupForTicket(ticket.Id);

        public Group? GetGroupForTicket(int ticketNumber)
        {
            return Context.Groups.FirstOrDefault(group => group.GroupTickets.Contains(ticketNumber));
        }
    }
}
