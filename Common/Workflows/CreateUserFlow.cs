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
    public class CreateUserFlow : Workflow
    {
        private SettingsService SettingsService { get; }

        public CreateUserFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, SettingsService settingsService) 
            : base(context, localizationService, ticketService)
        {
            SettingsService = settingsService;
        }

        public override (bool Succeeded, string Message) Commit()
        {
            return base.Commit();
        }
    }
}
