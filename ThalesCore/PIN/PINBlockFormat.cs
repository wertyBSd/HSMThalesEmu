using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.PIN
{
    public class PINBlockFormat
    {
        public enum PIN_Block_Format
        {
            AnsiX98 = 1,
            Docutel = 2,
            Diebold = 3,
            Plus = 4,
            ISO9564_1 = 5,
            InvalidPBCode = 9999
        }

        public static PIN_Block_Format ToPINBlockFormat(string PBFormat)
        {
            switch (PBFormat)
            {
                case "01":
                    return PIN_Block_Format.AnsiX98;
                case "02":
                    return PIN_Block_Format.Docutel;
                case "03":
                    return PIN_Block_Format.Diebold;
                case "04":
                    return PIN_Block_Format.Plus;
                case "05":
                    return PIN_Block_Format.ISO9564_1;
                default:
                    return PIN_Block_Format.InvalidPBCode;
            }
        }

        public static string FromPINBlockFormat(PIN_Block_Format ToPINBlockFormat)
        {
            switch (ToPINBlockFormat)
            {
                case PIN_Block_Format.AnsiX98:
                    return "01";
                case PIN_Block_Format.Diebold:
                    return "03";
                case PIN_Block_Format.Docutel:
                    return "02";
                case PIN_Block_Format.Plus:
                    return "04";
                case PIN_Block_Format.ISO9564_1:
                    return "05";
                default:
                    return "";
            }
        }

        public static string ToPINBlock(string ClearPIN, string AccountNumber_Or_PaddingString, PIN_Block_Format Format)
        {
            switch (Format)
            {
                case PIN_Block_Format.AnsiX98:
                    if (AccountNumber_Or_PaddingString.Length < 12) throw new Exceptions.XInvalidAccount("Account length must be equal or greater to 12");
                    string s1 = (ClearPIN.Length.ToString().PadLeft(2, '0') + ClearPIN).PadRight(16, 'F');
                    string s2 = AccountNumber_Or_PaddingString.PadLeft(16, '0');
                    return Utility.XORHexStrings(s1, s2);
                case PIN_Block_Format.Diebold:
                    return ClearPIN.PadRight(16, 'F');
                case PIN_Block_Format.Docutel:
                    if (AccountNumber_Or_PaddingString.Length < 6) throw new Exceptions.XInvalidAccount("Account length must be equal or greater to 6");
                    string s3 = ClearPIN.Length.ToString() + ClearPIN.PadLeft(6, '0');
                    return s3 + AccountNumber_Or_PaddingString.Substring(0, 16 - s3.Length);
                case PIN_Block_Format.Plus:
                    throw new Exceptions.XUnsupportedPINBlockFormat("Unsupported PIN block format PLUS");
                case PIN_Block_Format.ISO9564_1:
                    string s4 = ("0" + ClearPIN.Length.ToString() + ClearPIN).PadLeft(16, 'F');
                    string s5 = "0000" + AccountNumber_Or_PaddingString.Substring(0, 12);
                    return Utility.XORHexStrings(s4, s5);
                default:
                    throw new Exceptions.XUnsupportedPINBlockFormat("Unsupported PIN block format " + Format.ToString());
            }
        }

        public static string ToPIN(string PINBlock, string AccountNumber_Or_PaddingString, PIN_Block_Format Format)
        {
            switch (Format)
            {
                case PIN_Block_Format.AnsiX98:
                    string s2 = AccountNumber_Or_PaddingString.PadLeft(16, '0');
                    string s1 = Utility.XORHexStrings(s2, PINBlock);
                    int PINLength = Convert.ToInt32(s1.Substring(0, 2));
                    return s1.Substring(2, PINLength);
                case PIN_Block_Format.Diebold:
                    return PINBlock.Replace("F", "");
                case PIN_Block_Format.Docutel:
                    int PINLength2 = Convert.ToInt32(PINBlock.Substring(0, 1));
                    return PINBlock.Substring(1, PINLength2);
                case PIN_Block_Format.Plus:
                    throw new Exceptions.XUnsupportedPINBlockFormat("Unsupported PIN block format PLUS");
                case PIN_Block_Format.ISO9564_1:
                    string s4 = "0000" + AccountNumber_Or_PaddingString.Substring(0, 12);
                    string s5 = Utility.XORHexStrings(s4, PINBlock);
                    int PINLength3 = Convert.ToInt32(s5.Substring(11, 1));
                    return s5.Substring(12, PINLength3);
                default:
                    throw new Exceptions.XUnsupportedPINBlockFormat("Unsupported PIN block format " + Format.ToString());
            }
        }

    }
}
