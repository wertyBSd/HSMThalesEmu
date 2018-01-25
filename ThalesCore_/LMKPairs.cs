using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class LMKPairs
    {
        public const string LMK_PAIR_00_01 = "00_01";

        public const string LMK_PAIR_02_03 = "02_03";
        
        public const string LMK_PAIR_04_05 = "04_05";

        public const string LMK_PAIR_08_09 = "08_09";

        public const string LMK_PAIR_10_11 = "10_11";

        public const string LMK_PAIR_12_13 = "12_13";

        public const string LMK_PAIR_14_15 = "14_15";

        public const string LMK_PAIR_16_17 = "16_17";

        public const string LMK_PAIR_18_19 = "18_19";

        public const string LMK_PAIR_20_21 = "20_21";

        public const string LMK_PAIR_22_23 = "22_23";

        public const string LMK_PAIR_24_25 = "24_25";

        public const string LMK_PAIR_26_27 = "26_27";

        public const string LMK_PAIR_28_29 = "28_29";

        public const string LMK_PAIR_30_31 = "30_31";

        public const string LMK_PAIR_32_33 = "32_33";

        public const string LMK_PAIR_34_35 = "34_35";

        public const string LMK_PAIR_36_37 = "36_37";

        public const string LMK_PAIR_38_39 = "38_39";

        public static string[] LMK_PAIR_DESCRIPTION =  {"Contains the two smart card \"keys\" (Passwords if the HSM is configured for Password mode) required for setting the HSM into the Authorized state.",
                                                      "Encrypts the PINs for host storage.",
                                                      "Encrypts Zone Master Keys and double-length ZMKs. Encrypts Zone Master Key components under a Variant.",
                                                      "Encrypts the Zone PIN keys for interchange transactions.",
                                                      "Used for random number generation.",
                                                      "Used for encrypting keys in HSM buffer areas.",
                                                      "The initial set of Secret Values created by the user; used for generating all other Master Key pairs.",
                                                      "Encrypts Terminal Master Keys, Terminal PIN Keys and PIN Verification Keys. Encrypts Card Verification Keys under a Variant.",
                                                      "Encrypts Terminal Authentication Keys.",
                                                      "Encrypts reference numbers for solicitation mailers.",
                                                      "Encrypts 'not on us' PIN Verification Keys and Card Verification Keys under a Variant.",
                                                      "Encrypts Watchword Keys.",
                                                      "Encrypts Zone Transport Keys.",
                                                      "Encrypts Zone Authentication Keys.",
                                                      "Encrypts Terminal Derivation Keys.",
                                                      "Encrypts Zone Encryption Keys.",
                                                      "Encrypts Terminal Encryption Keys.",
                                                      "Encrypts RSA Keys.",
                                                      "Encrypts RSA MAC Keys.",
                                                      "LMK pair 38-39."};
        public enum LMKPair
        {
            Pair00_01 = 0,
            Pair02_03 = 1,
            Pair04_05 = 2,
            Pair06_07 = 3,
            Pair08_09 = 4,
            Pair10_11 = 5,
            Pair12_13 = 6,
            Pair14_15 = 7,
            Pair16_17 = 8,
            Pair18_19 = 9,
            Pair20_21 = 10,
            Pair22_23 = 11,
            Pair24_25 = 12,
            Pair26_27 = 13,
            Pair28_29 = 14,
            Pair30_31 = 15,
            Pair32_33 = 16,
            Pair34_35 = 17,
            Pair36_37 = 18,
            Pair38_39 = 19
        }

        public static void LMKTypeCodeToLMKPair(string s, LMKPair LMK, int var)
        {
            var = 0;
            if ((s == null) || (s == "")) return;

            switch (s)
            {
                case "00":
                    LMK = LMKPair.Pair04_05;
                    break;
                case "01":
                    LMK = LMKPair.Pair06_07;
                    break;
                case "02":
                    LMK = LMKPair.Pair14_15;
                    break;
                case "03":
                    LMK = LMKPair.Pair16_17;
                    break;
                case "04":
                    LMK = LMKPair.Pair18_19;
                    break;
                case "05":
                    LMK = LMKPair.Pair20_21;
                    break;
                case "06":
                    LMK = LMKPair.Pair22_23;
                    break;
                case "07":
                    LMK = LMKPair.Pair24_25;
                    break;
                case "08":
                    LMK = LMKPair.Pair26_27;
                    break;
                case "09":
                    LMK = LMKPair.Pair28_29;
                    break;
                case "0A":
                    LMK = LMKPair.Pair30_31;
                    break;
                case "0B":
                    LMK = LMKPair.Pair32_33;
                    break;
                case "10":
                    LMK = LMKPair.Pair04_05;
                    break;
                case "42":
                    LMK = LMKPair.Pair14_15;
                    break;
                default:
                    throw new Exception("Invalid LMKL type code [" + s + "]");
            }
        }

    }
}
