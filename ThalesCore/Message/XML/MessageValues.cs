using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.XML
{
    public class MessageKeyValuePairs
    {
        private SortedList<string, string> m_KVPairs = new SortedList<string, string>();

        public void Add(string key, string value)
        {
            m_KVPairs.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return m_KVPairs.ContainsKey(key);
        }

        public string Item(string key)
        {
            return m_KVPairs[key];
        }

        public string ItemOptional(string key)
        {
            return GetItemOrEmptyString(key);
        }

        public string ItemCombination(string key1, string key2)
        {
            return GetItemOrEmptyString(key1) + GetItemOrEmptyString(key2);
        }

        public int Count()
        {
            return m_KVPairs.Count();
        }

        public override string ToString()
        {
            StringBuilder strBld = new StringBuilder();
            foreach (String key in m_KVPairs.Keys)
                strBld.AppendFormat("[Key,Value]=[{0},{1}]{2}", key, m_KVPairs[key], System.Environment.NewLine);
            return strBld.ToString();
        }

        private string GetItemOrEmptyString(string key)
        {
            if (m_KVPairs.ContainsKey(key)) return m_KVPairs[key];
            else return "";
        }
    }

}
