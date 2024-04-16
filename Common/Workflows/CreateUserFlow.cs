using Common.DAL;
using Common.DAL.Models;
using Common.Enums;
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
        private UserService UserService { get; }
        private string Username { get; set; }
        private Role Role { get; set; }

        public CreateUserFlow(DepotContext context, LocalizationService localizationService, TicketService ticketService, UserService userService) 
            : base(context, localizationService, ticketService)
        {
        }

        public void SetUsername(string username)
        {
            Username = username;
        }

        public void SetRole(Role role)
        {
            Role = role;
        }

        public override (bool Succeeded, string Message) Commit()
        {
            UserService.AddUser(new User() { Name = Username, Role = (int)Role });

            return base.Commit();
        }
    }
}
