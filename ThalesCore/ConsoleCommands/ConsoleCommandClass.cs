using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public class ConsoleCommandClass
    {
        protected string m_commandCode;

        protected string m_commandDescription;

        protected Type m_commandType;

        public string CommandCode
        {
            get { return m_commandCode; }
            set { m_commandCode = value; }
        }

        public string CommandDescription
        {
            get { return m_commandDescription; }
            set { m_commandDescription = value; }
        }

        public Type CommandType
        {
            get { return m_commandType; }
            set { m_commandType = value; }
        }

        public ConsoleCommandClass(string commandCode, string commandDescription, Type commandType)
        {
            m_commandCode = commandCode;
            m_commandDescription = commandDescription;
            m_commandType = commandType;
        }
    }
}
