
using Common.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Choices
{
    public class TourChoice
    {
        public string Name { get; set; }
        public Tour Tour { get; set; }

        public TourChoice(string name, Tour tour)
        {
            Name = name;
            Tour = tour;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
