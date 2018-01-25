using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public class ConsoleMessageStack
    {
        protected Stack<ConsoleMessage> m_stack = new Stack<ConsoleMessage>();

        public void PushToStack(ConsoleMessage msg)
        {
            m_stack.Push(msg);
        }

        public ConsoleMessage PopFromStack()
        {
            return m_stack.Pop();
        }

        public int MessagesOnStack()
        {
            return m_stack.Count;
        }
    }
}
