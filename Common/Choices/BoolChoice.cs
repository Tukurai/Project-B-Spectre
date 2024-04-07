
using Common.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Choices
{
    public class BoolChoice
    {
        public string Name { get; set; }
        public bool Choice { get; set; }

        public BoolChoice(string name, bool choice)
        {
            Name = name;
            Choice = choice;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
