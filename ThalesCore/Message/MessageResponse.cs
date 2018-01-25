using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Message
{
    public class MessageResponse
    {
        private string _data = "";

        public string MessageData
        {
            get { return _data; }
        }

        public MessageResponse()
        {
        }

        public void AddElement(string s)
        {
            _data += s;
        }

        public void AddElementFront(string s)
        {
            _data = s + _data;
        }
    }
}
