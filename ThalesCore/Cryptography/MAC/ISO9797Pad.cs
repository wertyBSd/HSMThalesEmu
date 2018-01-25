using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography.MAC
{
    public class ISO9797Pad
    {
        public static string PadHexString(string dataStr, ISO9797PaddingMethods paddingMethod)
        {
            if (paddingMethod == ISO9797PaddingMethods.NoPadding)
                return dataStr;

            string firstPadString = "80";
            if (paddingMethod == ISO9797PaddingMethods.PaddingMethod1)
                firstPadString = "00";

            if ((int)(dataStr.Length / 2) % 8 == 0)
                return dataStr + firstPadString + "00000000000000";
            else
            {
                dataStr = dataStr + firstPadString;
                while ((int)(dataStr.Length / 2) % 8 != 0)
                    dataStr = dataStr + "00";
                return dataStr;
            }
         }
    }
}
