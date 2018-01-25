using System;
using System.Collections.Generic;

namespace HostCommands
{
    public class CommandExplorer
    {
        private SortedList<string,CommandClass> _commandTypes = new SortedList<string, CommandClass>();

        /// <summary>
        /// CommandExplorer constructor.
        /// </summary>
        /// <remarks>
        /// The constructor will search the loaded assemblies for classes that have the
        /// <see cref="HostCommands.ThalesCommandCode"/> attribute.
        /// </remarks>
        public CommandExplorer()
        {
            System.Reflection.Assembly[] asm = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asm.Length; i++)
            {
                foreach (Type t in asm[i].GetTypes())
                {
                    foreach (Attribute atr in t.GetCustomAttributes(false))
                    {
                        if (atr.GetType() == typeof(HostCommands.ThalesCommandCode))
                        {

                            try
                            {
                                ThalesCommandCode cccAttr = (ThalesCommandCode)atr;
                                _commandTypes.Add(cccAttr.CommandCode, new CommandClass(cccAttr.CommandCode, cccAttr.ResponseCode, cccAttr.ResponseCodeAfterIO, t, asm[i].FullName, cccAttr.Description));
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
            IEnumerator<KeyValuePair<String, CommandClass>> en = _commandTypes.GetEnumerator();

            while (en.MoveNext())
            {
                s += "Command code: " + en.Current.Value.CommandCode + System.Environment.NewLine +
                    "Response code: " + en.Current.Value.ResponseCode + System.Environment.NewLine +
                    "Type: " + en.Current.Value.DeclaringType.FullName +
                    "Description: " + en.Current.Value.Description + System.Environment.NewLine + System.Environment.NewLine;
            }
            en.Dispose();
            en = null;
            return s;
        }

        public CommandClass GetLoadedCommand(string commandCode)
        {
            try
            {
                return _commandTypes[commandCode];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public void ClearLoadedCommands()
        {
            _commandTypes.Clear();
        }

    }
}
