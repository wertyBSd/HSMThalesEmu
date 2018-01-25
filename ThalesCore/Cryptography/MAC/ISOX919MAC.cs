using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography.MAC
{
    public class ISOX919MAC
    {
        public static string MacHexData(string dataStr, HexKey key, string IV, ISOX919Blocks block)
        {
            if (dataStr.Length % 16 != 0)
                dataStr = Cryptography.MAC.ISO9797Pad.PadHexString(dataStr, Cryptography.MAC.ISO9797PaddingMethods.PaddingMethod1);

            string result = dataStr;

            for (int i = 0; i < (dataStr.Length / 16); i++)
            {
                IV = Utility.XORHexStringsFull(IV, dataStr.Substring(i * 16, 16));
                IV = Cryptography.DES.DESEncrypt(key.PartA, IV);
            }

            result = IV;

            if ((block == ISOX919Blocks.FinalBlock) || (block == ISOX919Blocks.OnlyBlock))
            {
                result = Cryptography.DES.DESDecrypt(key.PartB, IV);
                result = Cryptography.DES.DESEncrypt(key.PartA, result);
            }

            return result;
        }
    }
}
