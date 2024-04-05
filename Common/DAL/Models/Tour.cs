using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Models
{
    public class Tour : DbEntity
    {
        public DateTime Start { get; set; }
        public List<int> RegisteredTickets { get; set; } = new List<int>();
        public bool Departed { get; set; } = false;
    }
}
