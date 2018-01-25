using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostCommands
{
    public class CommandClass : IComparer
    {

        public string CommandCode;

        public string ResponseCode;

        public string ResponseCodeAfterIO;

        public Type DeclaringType;

        public string Description;

        public string AssemblyName;

        public CommandClass(string cCode, string rCode, string rCodeAfterIO, Type dclType, string assemblyName, string description)
        {
            this.CommandCode = cCode;
            this.ResponseCode = rCode;
            this.ResponseCodeAfterIO = rCodeAfterIO;
            this.DeclaringType = dclType;
            this.AssemblyName = assemblyName;
            this.Description = description;
        }

        public int Compare(object x, object y)
        {
            return String.Compare(((CommandClass)x).CommandCode, ((CommandClass)y).CommandCode);
        }
    }
}
