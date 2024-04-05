using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;

namespace Common.Static
{
    public class ConfigService : BaseService
    {
        public ConfigService(DepotContext context) : base(context)
        {
        }
    }
}
