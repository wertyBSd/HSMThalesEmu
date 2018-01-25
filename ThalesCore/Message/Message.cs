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

        public Message(byte[] data)
        {
            _data = Utility.GetStringFromBytes(data);
        }

        public void ResetIndex()
        {
            _curIndex = 0;
        }

        public void AdvanceIndex(int count)
        {
            _curIndex += count;
        }

        public void DecreaseIndex(int count)
        {
            _curIndex -= count;
        }

        public string GetSubstring(int length)
        {
            return _data.Substring(_curIndex, length);
        }

        public byte[] GetRemainingBytes()
        {
            byte[] b = new byte[_data.Length - _curIndex - 1];
            Array.Copy(_bData, _curIndex, b, 0, b.GetLength(0));
            return b;
        }

        public int CharsLeft()
        {
            return _data.Length - _curIndex;
        }

        public string GetTrailers()
        {
            _bData = Utility.GetBytesFromString(this.MessageData);
            int idx = _bData.GetLength(0) - 1;
            while (idx >= 0)
            {
                if (_bData[idx] == System.Convert.ToChar(System.Convert.ToUInt32("19", 16)))
                {
                    byte[] b = new byte[_bData.GetLength(0) - idx];
                    Array.Copy(_bData, idx, b, 0, b.GetLength(0));
                    byte[] bnew = new byte[idx];
                    Array.Copy(_bData, 0, bnew, 0, bnew.GetLength(0));
                    _bData = bnew;
                    _data = Utility.GetStringFromBytes(_bData);
                    return Utility.GetStringFromBytes(b);
                }
                idx -= 1;
            }

            return "";
        }
    }
}
