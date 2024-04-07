using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Models
{
    public class Group : DbEntity
    {
        public int GroupOwnerId { get; set; }
        public List<int> GroupTickets { get; set; } = new List<int>();
    }
}
