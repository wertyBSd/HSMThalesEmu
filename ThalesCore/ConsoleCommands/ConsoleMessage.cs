using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public class ConsoleMessage
    {
        protected string m_clientMessage;

        protected string m_consoleMessage;

        protected bool m_isNumberOfComponents;

        protected bool m_isComponent;

        protected IConsoleDataValidator m_messageValidator;

        public string ClientMessage
        {
            get { return m_clientMessage; }
            set { m_clientMessage = value; }
        }

        public string ConsoleMessageProperty
        {
            get { return m_consoleMessage; }
            set { m_consoleMessage = value; }
        }

        public bool IsNumberOfComponents
        {
            get { return m_isNumberOfComponents; }
            set { m_isNumberOfComponents = value; }
        }

        public bool IsComponent
        {
            get { return m_isComponent; }
            set { m_isComponent = value; }
        }

        public IConsoleDataValidator ConsoleMessageValidator
        {
            get { return m_messageValidator; }
            set { m_messageValidator = value; }
        }

        public ConsoleMessage(string clientMessage, string consoleMessage, IConsoleDataValidator messageValidator)
        {
            m_clientMessage = clientMessage;
            m_consoleMessage = consoleMessage;
            m_messageValidator = messageValidator;
            m_isNumberOfComponents = false;
            m_isComponent = false;
        }

        public ConsoleMessage(string clientMessage, string consoleMessage, bool isNumberOfComponents, IConsoleDataValidator messageValidator)
        {
            m_clientMessage = clientMessage;
            m_consoleMessage = consoleMessage;
            m_messageValidator = messageValidator;
            m_isNumberOfComponents = isNumberOfComponents;
            m_isComponent = false;
        }

        public ConsoleMessage(string clientMessage, string consoleMessage, bool isNumberOfComponents, bool isComponent, IConsoleDataValidator messageValidator)
        {
            m_clientMessage = clientMessage;
            m_consoleMessage = consoleMessage;
            m_messageValidator = messageValidator;
            m_isNumberOfComponents = isNumberOfComponents;
            m_isComponent = isComponent;
        }
    }
}
