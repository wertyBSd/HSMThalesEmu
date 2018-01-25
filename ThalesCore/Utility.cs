using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore
{
    public class Utility
    {
        private static Random rndMachine = new Random();

        public enum ParityCheck
        {
            OddParity = 0,
            EvenParity = 1,
            NoParity = 2
        }

        public static void HexStringToByteArray(string s, byte[] bData)
        {
            int i = 0;
            int j = 0;
            while (i <= s.Length - 1)
            {
                bData[j] = Convert.ToByte(s.Substring(i, 2), 16);
                i += 2;
                j += 1;
            }
        }

        public static void ByteArrayToHexString(byte[] bData, out String s)
        {
            StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < bData.GetUpperBound(0) + 1; i++)
            {
                sb.AppendFormat("{0:X2}", bData[i]);
            }
            s = sb.ToString();
            sb = null;
        }

        public static string RandomKey(bool EnforceParity, ParityCheck Parity)
        {
            StringBuilder sb = new StringBuilder();
            string s;
            for (int i = 1; i < 17; i++)
            {
                sb.AppendFormat("{0:X1}", rndMachine.Next(0, 16));
            }
            s = sb.ToString();
            sb = null;
            if (EnforceParity)
            {
                if (Parity != ParityCheck.NoParity)
                {
                    s = MakeParity(s, Parity);
                }
            }
            return s;
        }

        public static bool IsHexString(string s)
        {
            if (String.IsNullOrEmpty(s)) return false;
            s = RemoveKeyType(s).ToUpper();
            for (int i = 0; i < s.Length; i++)
            {
                if (!Char.IsDigit(s[i]))
                {
                    if ((s[i] < 'A') || (s[i] > 'F')) return false;
                }
            }
            return true;
        }

        public static bool IsHexString(string s, bool removeType)
        {
            if (String.IsNullOrEmpty(s)) return false;

            if (removeType) s = RemoveKeyType(s).ToUpper();

            for (int i = 0; i < s.Length; i++)
            {
                if (!Char.IsDigit(s[i]))
                {
                    if ((s[i] < 'A') || (s[i] > 'F')) return false;
                }
            }

            return true;
        }

        public static bool IsParityOK(string hexString, ParityCheck parity)
        {
            if (parity == ParityCheck.NoParity) return true;
            hexString = RemoveKeyType(hexString);
            int i = 0;
            while (i < hexString.Length)
            {
                string b = toBinary(hexString.Substring(i, 2));
                i += 2;
                int l = 0;
                for (int j = 0; j < b.Length; j++)
                {
                    if (b.Substring(j, 1) == "1") l += 1;
                }
                if (((l % 2 == 0) && (parity == ParityCheck.OddParity)) || ((l % 2 == 1) && (parity == ParityCheck.EvenParity))) return false;

            }
            return true;
        }

        public static string MakeParity(string hexString, ParityCheck parity)
        {
            if (parity == ParityCheck.NoParity) return hexString;

            string head = "";
            if (hexString != RemoveKeyType(hexString))
            {
                head = hexString.Substring(0, 1);
                hexString = RemoveKeyType(hexString);
            }

            int i = 0;
            string r = "";

            while (i < hexString.Length)
            {
                string b = toBinary(hexString.Substring(i, 2));
                i += 2;
                int l = b.Replace("0", "").Length;

                if (((l % 2 == 0) && (parity == ParityCheck.OddParity)) || ((l % 2 == 1) && (parity == ParityCheck.EvenParity)))
                {
                    if (b.Substring(7, 1) == "1")
                        r = r + b.Substring(0, 7) + "0";
                    else
                        r = r + b.Substring(0, 7) + "1";

                }
                else r = r + b;

            }

            return head + fromBinary(r);
        }


        public static string XORHexStrings(string s1, string s2)
        {
            return XORHexStringsFull(s1.Substring(0, 16), s2.Substring(0, 16));
        }

        public static string XORHexStringsFull(string s1, string s2)
        {
            string s = "";

            s1 = RemoveKeyType(s1);
            s2 = RemoveKeyType(s2);
            for (int i = 0; i < s1.Length; i++)
                s = s + (Convert.ToInt32(s1.Substring(i, 1), 16) ^ Convert.ToInt32(s2.Substring(i, 1), 16)).ToString("X");
            
            return s;
        }

        public static string ORHexStringsFull(string b, string mask, int offset)
        {
            int l;
            int i;
            string s = b;
            char[] z = new char[s.Length];
            z = s.ToCharArray();
            l = Math.Min(b.Length - offset, mask.Length);
            for (i = 0; i < l; i++)
            {
                z[offset] = Convert.ToChar(String.Format("{0:X}", Convert.ToInt32(s.Substring(offset, 1), 16) | Convert.ToInt32(mask.Substring(i, 1), 16)));
                offset += 1;
            }
            s = z.ToString();
            return s;
        }

        public static string SHRHexString(string b)
        {
            int r = Convert.ToInt32(b, 16);
            r = r >> 1;
            string s = b;
            s = String.Format("{0:X}", r).PadLeft(b.Length, '0');
            return s;
        }

        public static string ANDHexStrings(string b, string mask)
        {
            return ANDHexStringsOffset(b, mask, 0);
        }

        public static string ANDHexStringsOffset(string b, string mask, int offset)
        {
            int l;
            string s = b;
            char[] z = new char[s.Length];

            z = s.ToCharArray();
            l = Math.Min(b.Length - offset, mask.Length);

            for (int i = 0; i < l; i++)
            {
                z[offset] = Convert.ToChar(String.Format("{0:X}", (Convert.ToInt32(s.Substring(offset, 1), 16) & Convert.ToInt32(mask.Substring(i, 1), 16))));
                offset += 1;
            }
            s = z.ToString();
            return s;
        }

        private static bool arrayNotZero(byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
                if (b[i] != 0) return true;
            return false;
        }

        public static string toBinary(string hexString)
        {
            string r = "";
            for (int i = 0; i < hexString.Length; i++)
                r = r + Convert.ToString(Convert.ToInt32(hexString.Substring(i, 1), 16), 2).PadLeft(4, '0');
            return r;
        }

        public static string fromBinary(string binaryString)
        {
            string r = "";
            for (int i = 0; i < binaryString.Length; i += 4)
                r = r + Convert.ToByte(binaryString.Substring(i, 4), 2).ToString("X1");
            return r;
        }

        public static string fromBCD(string BCDString)
        {
            string r = "";
            for (int i = 0; i < BCDString.Length; i++)
            {
                byte b = Utility.GetBytesFromString(BCDString[i].ToString())[0];
                r = r + Convert.ToString((b >> 4)) + Convert.ToString((b & 15));
            }
            return r;
        }

        public static string toBCD(string decimalString)
        {

            string r = "";
            for (int i = 0; i < decimalString.Length; i += 2)
            {
                int b1 = Convert.ToInt32(decimalString[i]);
                int b2  = Convert.ToInt32(decimalString[i + 1]);
                r = r + Utility.GetStringFromBytes(new byte[] { Convert.ToByte(((b1 & 15) << 4) | (b2 & 15)) });
            }
            return r;
        }

        public static string AddNoCarry(string str1, string str2)
        {
            string output = "";
            for (int cnt = 0; cnt < str1.Length; cnt++)
                output = output + Convert.ToString((Convert.ToInt32(str1.Substring(cnt, 1)) + Convert.ToInt32(str2.Substring(cnt, 1))) % 10);
            return output;
        }

        public static string SubtractNoBorrow(string str1, string str2)
        {
            string output = "";
            int i;
            for (int cnt = 0; cnt < str1.Length; cnt++)
            {
                i = Convert.ToInt32(str1.Substring(cnt, 1)) - Convert.ToInt32(str2.Substring(cnt, 1));
                if (i < 0) i = 10 + i;
                else output = output + Convert.ToString(i);
            }
            return output;
        }

        public static string Decimalise(string undecimalisedString, string decimalisationTable)
        {
            const string EMPTY_DEC_TABLE = "FFFFFFFFFFFFFFFF";
            const string DEFAULT_DEC_TABLE = "9876543210123456";
            if (decimalisationTable == EMPTY_DEC_TABLE)
                decimalisationTable = DEFAULT_DEC_TABLE;
            string output = "";
            for (int cnt = 0; cnt < undecimalisedString.Length; cnt++)
            {
                char ch = undecimalisedString[cnt];
                if ((ch >= '0') & (ch <= '9'))
                    output = output + ch;
                else
                {
                    int rep_index = (GetBytesFromString(ch.ToString())[0] - 65) + 10;
                    output = output + decimalisationTable[rep_index];
                }
            }
            return output;
        }

        public static string RemoveKeyType(string keyString)
        {
            if (String.IsNullOrEmpty(keyString)) return keyString;
            if (keyString.StartsWith(KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi)) ||
                keyString.StartsWith(KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyVariant)) ||
                keyString.StartsWith(KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.SingleDESKey)) ||
                keyString.StartsWith(KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyAnsi)) ||
                keyString.StartsWith(KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyVariant)))
                return keyString.Substring(1);
            else
                return keyString;
        }

        public static string CreateRandomKey(KeySchemeTable.KeyScheme ks)
        {
            switch (ks)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                    return Utility.RandomKey(true, Utility.ParityCheck.OddParity);
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                    return Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    return Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                default:
                    throw new InvalidOperationException("Invalid key scheme [" + ks.ToString() + "]");
            }
        }

        public static string EncryptUnderLMK(string clearKey, KeySchemeTable.KeyScheme Target_KeyScheme, LMKPairs.LMKPair LMKKeyPair, string variantNumber)
        {
            string result = "";

            switch (Target_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.Unspecified:
                    result = TripleDES.TripleDESEncrypt(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), clearKey);
                    break;
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = TripleDES.TripleDESEncryptVariant(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), clearKey);
                    break;
                default:
                    //throw new InvalidOperationException("Invalid key scheme [" + Target_KeyScheme.ToString() + "]");
                    break;
            }

            switch (Target_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = KeySchemeTable.GetKeySchemeValue(Target_KeyScheme) + result;
                    break;
                default:
                    break;
            }
            return result;
        }

        public static string DecryptUnderLMK(string encryptedKey, KeySchemeTable.KeyScheme Target_KeyScheme, LMKPairs.LMKPair LMKKeyPair, string variantNumber)
        {
            string result = "";

            switch (Target_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.Unspecified:
                    result = TripleDES.TripleDESDecrypt(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), encryptedKey);
                    break;
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = TripleDES.TripleDESDecryptVariant(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), encryptedKey);
                    break;
                default:
                    break;
            }


            switch (Target_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = KeySchemeTable.GetKeySchemeValue(Target_KeyScheme) + result;
                    break;
                default:
                    break;
            }

            return result;
        }

        public static string DecryptZMKEncryptedUnderLMK(string encryptedZMK, KeySchemeTable.KeyScheme ks, int var)
        {
            switch (ks)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                    return TripleDES.TripleDESDecrypt(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKPairs.LMKPair.Pair04_05, var)), encryptedZMK);
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    return TripleDES.TripleDESDecryptVariant(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKPairs.LMKPair.Pair04_05, var)), encryptedZMK);
                default:
                    throw new InvalidOperationException("Invalid key scheme [" + ks.ToString() + "]");
            }
        }

        public static string EncryptUnderZMK(string clearZMK, string clearData, KeySchemeTable.KeyScheme ZMK_KeyScheme)
        {
            return EncryptUnderZMK(clearZMK, clearData, ZMK_KeyScheme, String.Empty);
        }

        public static string EncryptUnderZMK(string clearZMK, string clearData, KeySchemeTable.KeyScheme ZMK_KeyScheme, string AtallaVariant)
        {
            string result = "";

            clearZMK = Utility.TransformUsingAtallaVariant(clearZMK, AtallaVariant);

            switch (ZMK_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.Unspecified:
                    result = TripleDES.TripleDESEncrypt(new HexKey(clearZMK), clearData);
                    break;
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = TripleDES.TripleDESEncryptVariant(new HexKey(clearZMK), clearData);
                    break;
                default:
                    break;
            }

            switch (ZMK_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = KeySchemeTable.GetKeySchemeValue(ZMK_KeyScheme) + result;
                    break;
                default:
                    break;
            }

            return result;
        }

        public static string AppendDirectorySeparator(string path)
        {
            char endChar;
            if (path.IndexOf('\'') > -1)
                endChar = '\'';
            else
                endChar = '/';

            if (path.EndsWith(endChar.ToString()))
                return path;
            else
                return path + endChar;
        }

        public static string getTimeMMHHSSmmmm()
        {
            return DateTime.Now.Hour.ToString().PadLeft(2, '0') + ":" + DateTime.Now.Minute.ToString().PadLeft(2, '0')
                + ":" + DateTime.Now.Second.ToString().PadLeft(2, '0') + "." + DateTime.Now.Millisecond.ToString().PadLeft(3, '0');
        }

        public static bool TryParseInt(string str)
        {
            try
            {
                int intS = Convert.ToInt32(str);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetExecutingDirectory()
        {
            string exePathh = System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
            if (exePathh.IndexOf('\\') > -1)
                return exePathh.Substring(0, exePathh.LastIndexOf('\\'));
            else
                return exePathh.Substring(0, exePathh.LastIndexOf('/'));
        }

        public static byte[] GetBytesFromString(string str)
        {
            return GetBytesFromString(str, System.Text.ASCIIEncoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage));
        }

        public static byte[] GetBytesFromString(string str, System.Text.Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static string GetStringFromBytes(byte[] b)
        {
            return System.Text.ASCIIEncoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage).GetChars(b).ToString();
        }

        public static string TransformUsingAtallaVariant(string key, string AtallaVariant)
        {
            if ((AtallaVariant == String.Empty) || (AtallaVariant == ""))
                return key;
            int var = Convert.ToInt32(AtallaVariant);
            int varLen = (int)new System.ComponentModel.Int32Converter().ConvertFromString("0xH8") * var;
            string varStr = String.Empty;
            if (varLen != 8)
                varStr = Convert.ToString(varLen, 16).PadRight(16, '0');
            else
                varStr = "08".PadRight(16, '0');
            HexKey hKey = new HexKey(key);
            hKey.PartA = Utility.XORHexStrings(hKey.PartA, varStr);

            if (hKey.KeyLen != HexKey.KeyLength.SingleLength)
            {
                hKey.PartB = Utility.XORHexStrings(hKey.PartB, varStr);
                if (hKey.KeyLen != HexKey.KeyLength.DoubleLength)
                    hKey.PartC = Utility.XORHexStrings(hKey.PartC, varStr);
            }
            return hKey.ToString();
        }
    }
}
