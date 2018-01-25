using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Exceptions
{
    public class XNeedsAuthorizedState : Exception
    {
        public XNeedsAuthorizedState(string description) : base(description)
        {

        }
    }
}
