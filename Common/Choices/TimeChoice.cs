
using Common.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Choices
{
    public class TimeChoice
    {
        public TimeSpan Span { get; set; }

        public TimeChoice(TimeSpan span)
        {
            Span = span;
        }

        public override string ToString()
        {
            return Span.ToString("hh\\:mm");
        }
    }
}
