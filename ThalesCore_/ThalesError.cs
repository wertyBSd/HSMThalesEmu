using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class ThalesError
    {
        private string _errorCode;

        private string _errorHelp;

        public string ErrorCode
        {
            get { return _errorCode; }
        }

        public string ErrorHelp
        {
            get { return _errorHelp; }
        }

        public ThalesError(string ErrorCode, string ErrorHelp)
        {
            _errorCode = ErrorCode;
            _errorHelp = ErrorHelp;
        }
    }
}
