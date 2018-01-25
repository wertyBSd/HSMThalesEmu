using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Exceptions
{
    public class XInvalidKey : Exception
    {
        public XInvalidKey(string description) : base(description)
        {

        }
    }
}
