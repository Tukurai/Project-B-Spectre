using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;

namespace Common.Services
{
    public class SettingsService : BaseService
    {
        public SettingsService(DepotContext context) : base(context) { }

        public int? GetValueAsInt(string setting)
        {
            return int.TryParse(GetValue(setting), out int value) ? value : null;
        }

        public string? GetValue(string setting)
        {
            return Context.Settings.FirstOrDefault(s => s.Key == setting)?.Value;
        }   
    }
}
