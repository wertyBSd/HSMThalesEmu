using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.Message
{
    public class Message
    {
        private string _data = "";

        private byte[] _bData;

        private int _curIndex = 0;

        public string MessageData
        {
            get { return _data; }
        }

        public int CurrentIndex
        {
            get { return _curIndex; }
        }

        public Message(string data)
        {
            _bData = Utility.GetBytesFromString(data);
        }
    }
}
