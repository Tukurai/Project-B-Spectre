using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Models
{
    public class Ticket : DbEntity
    {
        public DateTime ValidOn { get; set; }
        public bool Expires { get; set; } = false;
    }
}
