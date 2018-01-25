using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography
{
    public class TripleDES
    {
        public static string TripleDESEncrypt(HexKey key, string data)
        {
            if ((data == null) || ((data.Length != 16) && (data.Length != 32) && (data.Length != 48)))
                throw new Exceptions.XInvalidData("Invalid data for 3DEncrypt");
            string result;
            if (data.Length == 16)
                result = TripleDESEncryptSingleLength(key, data);
            else if (data.Length == 32)
                result = TripleDESEncryptSingleLength(key, data.Substring(0, 16)) +
                         TripleDESEncryptSingleLength(key, data.Substring(16, 16));
            else
                result = TripleDESEncryptSingleLength(key, data.Substring(0, 16)) +
                         TripleDESEncryptSingleLength(key, data.Substring(16, 16)) +
                         TripleDESEncryptSingleLength(key, data.Substring(32, 16));
           
            return result;
        }

        private static string TripleDESEncryptSingleLength(HexKey key, string data)
        {
            string result = "";
            result = DES.DESEncrypt(key.PartA, data);
            result = DES.DESDecrypt(key.PartB, result);
            result = DES.DESEncrypt(key.PartC, result);
            return result;
        }

        public static string TripleDESDecrypt(HexKey key, string data)
        {
            if ((data == null) || ((data.Length != 16) && (data.Length != 32) && (data.Length != 48)))
                throw new Exceptions.XInvalidData("Invalid data for 3DEncrypt");
            string result;
            if (data.Length == 16)
                result = TripleDESDecryptSingleLength(key, data);
            else if (data.Length == 32)
                result = TripleDESDecryptSingleLength(key, data.Substring(0, 16)) +
                         TripleDESDecryptSingleLength(key, data.Substring(16, 16));
            else
                result = TripleDESDecryptSingleLength(key, data.Substring(0, 16)) +
                         TripleDESDecryptSingleLength(key, data.Substring(16, 16)) +
                         TripleDESDecryptSingleLength(key, data.Substring(32, 16));

            return result;
        }

        internal static string TripleDESEncrypt(HexKey hexKey, object zEROES)
        {
            throw new NotImplementedException();
        }

        private static string TripleDESDecryptSingleLength(HexKey key, string data)
        {
            string result = "";
            result = DES.DESDecrypt(key.PartC, data);
            result = DES.DESEncrypt(key.PartB, result);
            result = DES.DESDecrypt(key.PartA, result);
            return result;
        }

        public static string TripleDESEncryptVariant(HexKey key, string data)
        {
            if ((data == null) || ((data.Length != 32) && (data.Length != 48)))
                throw new Exceptions.XInvalidData("Invalid data for 3DEncrypt with variant");

            string result1;
            string result2;
            string result3 = "";
            string orgKeyPartB;

            if (data.Length == 32)
            {
                orgKeyPartB = key.PartB;
                key.PartB = Utility.XORHexStrings(key.PartB, Cryptography.LMK.Variants.DoubleLengthVariant(1).PadRight(16, '0'));
                result1 = TripleDESEncrypt(key, data.Substring(0, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.DoubleLengthVariant(2).PadRight(16, '0'));
                result2 = TripleDESEncrypt(key, data.Substring(16, 16));
            }
            else
            {
                orgKeyPartB = key.PartB;
                key.PartB = Utility.XORHexStringsFull(key.PartB, Cryptography.LMK.Variants.TripleLengthVariant(1).PadRight(16, '0'));
                result1 = TripleDESEncrypt(key, data.Substring(0, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.TripleLengthVariant(2).PadRight(16, '0'));
                result2 = TripleDESEncrypt(key, data.Substring(16, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.TripleLengthVariant(3).PadRight(16, '0'));
                result3 = TripleDESEncrypt(key, data.Substring(32, 16));
            }
            return result1 + result2 + result3;
        }

        public static string TripleDESDecryptVariant(HexKey key, string data)
        {
            if ((data == null) || ((data.Length != 32) && (data.Length != 48)))
                throw new Exceptions.XInvalidData("Invalid data for 3DDecrypt with variant");

            string result1;
            string result2;
            string result3 = "";
            string orgKeyPartB;

            if (data.Length == 32)
            {
                orgKeyPartB = key.PartB;
                key.PartB = Utility.XORHexStrings(key.PartB, Cryptography.LMK.Variants.DoubleLengthVariant(1).PadRight(16, '0'));
                result1 = TripleDESDecrypt(key, data.Substring(0, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.DoubleLengthVariant(2).PadRight(16, '0'));
                result2 = TripleDESDecrypt(key, data.Substring(16, 16));
            }
            else
            {
                orgKeyPartB = key.PartB;
                key.PartB = Utility.XORHexStringsFull(key.PartB, Cryptography.LMK.Variants.TripleLengthVariant(1).PadRight(16, '0'));
                result1 = TripleDESDecrypt(key, data.Substring(0, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.TripleLengthVariant(2).PadRight(16, '0'));
                result2 = TripleDESDecrypt(key, data.Substring(16, 16));
                key.PartB = Utility.XORHexStringsFull(orgKeyPartB, Cryptography.LMK.Variants.TripleLengthVariant(3).PadRight(16, '0'));
                result3 = TripleDESDecrypt(key, data.Substring(32, 16));
            }
            return result1 + result2 + result3;
        }
    }
}
