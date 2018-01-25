using System;
using System.Collections.Generic;

namespace HostCommands
{
    public class CommandExplorer
    {
        private SortedList<string,CommandClass> _commandTypes;


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
            for (int i = 0; i < asm.GetUpperBound(0); i++)
            {
                foreach (Type t in asm[i].GetTypes())
                {
                    foreach (Attribute atr in t.GetCustomAttributes(false))
                    {
                       //if(atr.GetType() is GetType(ThalesCommandCode))
                    }
                }
            }
        }
    }
}
