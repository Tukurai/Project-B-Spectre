using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;

namespace Common.Static
{
    public class BaseService
    {
        public DepotContext Context { get; }

        public BaseService(DepotContext context)
        {
            Context = context;
        }
    }
}
