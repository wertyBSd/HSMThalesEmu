using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Exceptions
{
    public class XInvalidComponentType : Exception
    {
        public XInvalidComponentType(string description) : base(description)
        {

        }
    }
}
