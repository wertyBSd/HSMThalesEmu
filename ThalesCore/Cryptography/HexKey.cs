using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography
{
    public class HexKey
    {
        public enum KeyLength
        {
            SingleLength = 0,
            DoubleLength = 1,
            TripleLength = 2
        }

        private string _partA;
        private string _partB;
        private string _partC;
        private KeyLength _keyLength;
        private KeySchemeTable.KeyScheme _scheme;
        
        public string PartA
        {
            get { return _partA; }
            set { _partA = value; }
        }

        public string PartB
        {
            get { return _partB; }
            set { _partB = value; }
        }

        public string PartC
        {
            get { return _partC; }
            set { _partC = value; }
        }

        public KeyLength KeyLen
        {
            get { return _keyLength; }
            set { _keyLength = value; }
        }

        public KeySchemeTable.KeyScheme Scheme
        {
            get { return _scheme; }
            set { _scheme = value; }
        }

        public HexKey(string key)
        {
            if ((key == null) || (key == ""))
            {
                throw new Exceptions.XInvalidKey("Invalid key data: " + key);
            }

            if ((key.Length == 17) || (key.Length == 33) || (key.Length == 49))
            {
                _scheme = KeySchemeTable.GetKeySchemeFromValue(key.Substring(0, 1));
                if ((_scheme != KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi)
                    && (_scheme != KeySchemeTable.KeyScheme.DoubleLengthKeyVariant)
                    && (_scheme != KeySchemeTable.KeyScheme.SingleDESKey)
                    && (_scheme != KeySchemeTable.KeyScheme.TripleLengthKeyAnsi)
                    && (_scheme != KeySchemeTable.KeyScheme.TripleLengthKeyVariant))
                    throw new Exceptions.XInvalidKeyScheme("Invalid key scheme " + key.Substring(0, 1));
                else
                    key = key.Substring(1);

            }
            else
            {
                if (key.Length == 16)
                    _scheme = KeySchemeTable.KeyScheme.SingleDESKey;
                else if (key.Length == 32)
                    _scheme = KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi;
                else
                    _scheme = KeySchemeTable.KeyScheme.TripleLengthKeyAnsi;

            }

            if (Utility.IsHexString(key) == false)
                throw new Exceptions.XInvalidKey("Invalid key data: " + key);

            if (key.Length == 16)
            {
                _partA = key;
                _partB = key;
                _partC = key;
                _keyLength = KeyLength.SingleLength;
            }
            else if (key.Length == 32)
            {
                _partA = key.Substring(0, 16);
                _partB = key.Substring(16);
                _partC = _partA;
                _keyLength = KeyLength.DoubleLength;
            }
            else if (key.Length == 48)
            {
                _partA = key.Substring(0, 16);
                _partB = key.Substring(16, 16);
                _partC = key.Substring(32);
                _keyLength = KeyLength.TripleLength;
            }
            else
                throw new Exceptions.XInvalidKey("Invalid key length: " + key);

        }
        public override string ToString()
        {
            if (_keyLength == KeyLength.SingleLength)
                return _partA;
            else if (_keyLength == KeyLength.DoubleLength)
                return _partA + _partB;
            else
                return _partA + _partB + _partC;
            
        }
    }
}
