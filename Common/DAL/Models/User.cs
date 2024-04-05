using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Models
{
    public class User : DbEntity
    {
        public int Role { get; set; }
        public string Name { get; set; } = "";
    }
}
