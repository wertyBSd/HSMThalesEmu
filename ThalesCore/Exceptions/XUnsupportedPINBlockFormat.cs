using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Exceptions
{
    public class XUnsupportedPINBlockFormat: Exception
    {
        public XUnsupportedPINBlockFormat(string description) : base(description)
        { }
    }
}
