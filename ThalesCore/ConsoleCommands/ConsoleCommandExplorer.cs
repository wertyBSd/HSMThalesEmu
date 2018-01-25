using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public class ConsoleCommandExplorer
    {
        private SortedList<string, ConsoleCommandClass> _consoleCommandTypes = new SortedList<string, ConsoleCommandClass>();

        public ConsoleCommandExplorer()
        {
            Assembly[] asm = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asm.GetUpperBound(0); i++)
            {
                foreach (Type t in asm[i].GetTypes())
                {
                    foreach (Attribute atr in t.GetCustomAttributes(false))
                    {
                        if (atr.GetType() == typeof(ThalesConsoleCommandCode))
                        {
                            try
                            {
                                ThalesConsoleCommandCode cccAttr = (ThalesConsoleCommandCode)atr;
                                _consoleCommandTypes.Add(cccAttr.ConsoleCommandCode, new ConsoleCommandClass(cccAttr.ConsoleCommandCode, cccAttr.Description, t));
                            }
                            catch (ArgumentException ex)
                            {
                            }
                        }
                    }
                }
            }

        }

        public string GetLoadedCommands()
        {
            string s = "";
            IEnumerator<KeyValuePair<String, ConsoleCommandClass>> en = _consoleCommandTypes.GetEnumerator();

            while (en.MoveNext())
            {
                s += "Command code: " + en.Current.Value.CommandCode + System.Environment.NewLine +
                    "Description: " + en.Current.Value.CommandDescription + System.Environment.NewLine + System.Environment.NewLine;
            }
            en.Dispose();
            en = null;
            return s;
        }

        public ConsoleCommandClass GetLoadedCommand(string commandCode)
        {
            try
            {
                return _consoleCommandTypes[commandCode];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public void ClearLoadedCommands()
        {
            _consoleCommandTypes.Clear();
        }
    }
}
