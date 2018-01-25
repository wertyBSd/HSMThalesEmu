using Message.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Message.XML
{
    public class MessageFieldsStore
    {
        private static SortedList<string, MessageFields> m_store = new SortedList<string, MessageFields>();

        public static void Clear()
        {
            m_store.Clear();
        }

        public static void Add(string key, MessageFields fields)
        {
            m_store.Add(key, fields);
        }

        public static void Remove(string key)
        {
            m_store.Remove(key);
        }

        public static MessageFields Item(string key)
        {
            if (m_store.ContainsKey(key))
                return m_store[key];
            else
                return null;
        }
    }
}
