using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography.LMK
{
    public class LMKStorage
    {
        private const int MAX_LMKS = 20;
        private static string[] _LMKs;
        private static string[] _LMKsOld;
        private static string[] _LMKsnew;
        private static string _storageFile = "";
        private static string _storageFileOld = "";
        private static bool _useOldStorage = false;

        public static string LMKStorageFile
        {
            get { return _storageFile; }
            set
            {
                _storageFile = value;
                _storageFileOld = value + ".old";
            }
        }

        public static string LMKOldStorageFile
        {
            get { return _storageFileOld; }
        }

        public static bool UseOldLMKStorage
        {
            get { return _useOldStorage; }
            set { _useOldStorage = value; }
        }

        public static string LMK(LMKPairs.LMKPair pair)
        {
            if (!UseOldLMKStorage) return _LMKs[Convert.ToInt32(pair)];
            else return _LMKsOld[Convert.ToInt32(pair)];
        }

        public static string LMKVariant(LMKPairs.LMKPair pair, int v)
        {
            string s = LMK(pair);
            if (v == 0) return s;
            string var = Cryptography.LMK.Variants.VariantNbr(v).PadRight(32, '0');
            return Utility.XORHexStringsFull(s, var);
        }

        public static void ReadLMKs(string StorageFile)
        {
            LMKStorageFile = StorageFile;
            ReadLMKs(LMKStorageFile, _LMKs);
            ReadLMKs(LMKOldStorageFile, _LMKsOld);
        }

        public static void GenerateLMKs()
        {
            if(_storageFile == "") throw new Exceptions.XInvalidStorageFile("Invalid storage file specified, value=" + _storageFile);

            _LMKs = new string[MAX_LMKS];
            _LMKsOld = new string[MAX_LMKS];

            for (int i = 0; i < MAX_LMKS; i++)
                _LMKs[i] = Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity);

            for (int i = 0; i < MAX_LMKS; i++)
                _LMKsOld[i] = Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity);

            WriteLMKs();
        }

        public static void GenerateTestLMKs()
        {
            if (_storageFile == "") throw new Exceptions.XInvalidStorageFile("Invalid storage file specified, value=" + _storageFile);
            _LMKs = new string[MAX_LMKS];
            _LMKsOld = new string[MAX_LMKS];

            _LMKs[0] = "01010101010101017902CD1FD36EF8BA";
            _LMKs[1] = "20202020202020203131313131313131";
            _LMKs[2] = "40404040404040405151515151515151";
            _LMKs[3] = "61616161616161617070707070707070";
            _LMKs[4] = "80808080808080809191919191919191";
            _LMKs[5] = "A1A1A1A1A1A1A1A1B0B0B0B0B0B0B0B0";
            _LMKs[6] = "C1C1010101010101D0D0010101010101";
            _LMKs[7] = "E0E0010101010101F1F1010101010101";
            _LMKs[8] = "1C587F1C13924FEF0101010101010101";
            _LMKs[9] = "01010101010101010101010101010101";
            _LMKs[10] = "02020202020202020404040404040404";
            _LMKs[11] = "07070707070707071010101010101010";
            _LMKs[12] = "13131313131313131515151515151515";
            _LMKs[13] = "16161616161616161919191919191919";
            _LMKs[14] = "1A1A1A1A1A1A1A1A1C1C1C1C1C1C1C1C";
            _LMKs[15] = "23232323232323232525252525252525";
            _LMKs[16] = "26262626262626262929292929292929";
            _LMKs[17] = "2A2A2A2A2A2A2A2A2C2C2C2C2C2C2C2C";
            _LMKs[18] = "2F2F2F2F2F2F2F2F3131313131313131";
            _LMKs[19] = "01010101010101010101010101010101";

            _LMKsOld[0] = "101010101010101F7902CD1FD36EF8BA";
            _LMKsOld[1] = "202020202020202F3131313131313131";
            _LMKsOld[2] = "404040404040404F5151515151515151";
            _LMKsOld[3] = "616161616161616E7070707070707070";
            _LMKsOld[4] = "808080808080808F9191919191919191";
            _LMKsOld[5] = "A1A1A1A1A1A1A1AEB0B0B0B0B0B0B0B0";
            _LMKsOld[6] = "C1C101010101010ED0D0010101010101";
            _LMKsOld[7] = "E0E001010101010EF1F1010101010101";
            _LMKsOld[8] = "1C587F1C13924FFE0101010101010101";
            _LMKsOld[9] = "010101010101010E0101010101010101";
            _LMKsOld[10] = "020202020202020E0404040404040404";
            _LMKsOld[11] = "070707070707070E1010101010101010";
            _LMKsOld[12] = "131313131313131F1515151515151515";
            _LMKsOld[13] = "161616161616161F1919191919191919";
            _LMKsOld[14] = "1A1A1A1A1A1A1A1F1C1C1C1C1C1C1C1C";
            _LMKsOld[15] = "232323232323232F2525252525252525";
            _LMKsOld[16] = "262626262626262F2929292929292929";
            _LMKsOld[17] = "2A2A2A2A2A2A2A2F2C2C2C2C2C2C2C2C";
            _LMKsOld[18] = "2F2F2F2F2F2F2FFE3131313131313131";
            _LMKsOld[19] = "010101010101010E0101010101010101";

            WriteLMKs();
        }

        public static string GenerateLMKCheckValue()
        {
            if (_storageFile == "") throw new Exceptions.XInvalidStorageFile("Invalid storage file specified");

            string s = Utility.XORHexStringsFull(_LMKs[0], _LMKs[1]);
            for (int i = 2; i < _LMKs.GetUpperBound(0); i++)
                s = Utility.XORHexStringsFull(_LMKs[i], s);


            return Utility.XORHexStrings(s.Substring(0, 16), s.Substring(16, 16));
        }

        public static bool CheckLMKStorage()
        {
            for (int i = 0; i < MAX_LMKS; i++)
            {
                if (Utility.IsParityOK(_LMKs[i], Utility.ParityCheck.OddParity) == false)
                    return false;
            }
            return true;
        }

        public static string DumpLMKs()
        {
            string s = "";
            for (int i = 0; i < MAX_LMKS; i++)
                s += _LMKs[i] + System.Environment.NewLine;
            return s;
        }

        private static void WriteLMKs()
        {
            WriteLMKs(LMKStorageFile, _LMKs);
            WriteLMKs(LMKOldStorageFile, _LMKsOld);
        }

        private static void WriteLMKs(string fileName, string[] LMKAr)
        {
            using (System.IO.StreamWriter SW = new System.IO.StreamWriter(fileName))
            {
                SW.WriteLine("; LMK Storage file");
                for(int i = 0; i < MAX_LMKS; i++)
                    SW.WriteLine(LMKAr[i]);
            }
        }

        private static void ReadLMKs(string fileName, string[] LMKAr)
        {
            int i = 0;
            LMKAr = new string[MAX_LMKS];

            try
            {
                using (System.IO.StreamReader SR = new System.IO.StreamReader(fileName))
                {
                    while (SR.Peek() > -1)
                    {
                        string s = SR.ReadLine();
                        if ((s != "") && (s.Trim().StartsWith(";")) == false)
                            if (Utility.IsHexString(s) == true)
                                if (s.Length == 32)
                                {
                                    LMKAr[i] = s;
                                    i += 1;
                                }
                    }
                }
            }
            catch (Exception ex)
            {
                Array.Clear(LMKAr, 0, MAX_LMKS);
            }
        }


    }
}
