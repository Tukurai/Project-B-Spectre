
using Common.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Choices
{
    public class DateChoice
    {
        public DateTime Date { get; set; }

        public DateChoice(DateTime date)
        {
            Date = date;
        }

        public override string ToString()
        {
            return Date.ToShortDateString();
        }
    }
}
