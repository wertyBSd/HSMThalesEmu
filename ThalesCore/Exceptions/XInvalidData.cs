using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Exceptions
{
    public class XInvalidData: Exception
    {
        public XInvalidData(string description) : base(description)
        {
        }
    }
}
