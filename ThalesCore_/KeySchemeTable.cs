using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class KeySchemeTable
    {
        public enum KeyScheme
        {
            SingleDESKey = 0,
            DoubleLengthKeyVariant = 1,
            TripleLengthKeyVariant = 2,
            DoubleLengthKeyAnsi = 3,
            TripleLengthKeyAnsi = 4,
            Unspecified = 5
        }

        public static string GetKeySchemeValue(KeyScheme key)
        {
            switch (key)
            {
                case KeyScheme.DoubleLengthKeyAnsi:
                    return "X";
                case KeyScheme.DoubleLengthKeyVariant:
                    return "U";
                case KeyScheme.SingleDESKey:
                    return "Z";
                case KeyScheme.TripleLengthKeyAnsi:
                    return "Y";
                case KeyScheme.TripleLengthKeyVariant:
                    return "T";
                default:
                    throw new Exception("Invalid key scheme");
            }
        }
        public static KeyScheme GetKeySchemeFromValue(string v)
        {
            switch (v)
            {
                case "X":
                    return KeyScheme.DoubleLengthKeyAnsi;
                case "U":
                    return KeyScheme.DoubleLengthKeyVariant;
                case "Z":
                    return KeyScheme.SingleDESKey;
                case "Y":
                    return KeyScheme.TripleLengthKeyAnsi;
                case "T":
                    return KeyScheme.TripleLengthKeyVariant;
                case "0":
                    return KeyScheme.Unspecified;
                default:
                    throw new Exception("Invalid key scheme " + v);
            }
        }

    }
}
