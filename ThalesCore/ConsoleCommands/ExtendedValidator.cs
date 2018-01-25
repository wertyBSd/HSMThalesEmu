using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public class ExtendedValidator : IConsoleDataValidator
    {

        protected ExtendedValidator m_nextVal = null;

        protected IConsoleDataValidator m_currentVal;

        public ExtendedValidator(IConsoleDataValidator validator)
        {
            m_currentVal = validator;
        }

        public ExtendedValidator Add(IConsoleDataValidator nextValidator)
        {
            m_nextVal = new ExtendedValidator(nextValidator);
            return m_nextVal;
        }

        public ExtendedValidator AddLast(IConsoleDataValidator nextValidator)
        {
            m_nextVal = new ExtendedValidator(nextValidator);
            return this;
        }

        public void ValidateConsoleMessage(string consoleMsg)
        {
            m_currentVal.ValidateConsoleMessage(consoleMsg);
            if (m_nextVal != null)
                m_nextVal.ValidateConsoleMessage(consoleMsg);
        }
    }
}
