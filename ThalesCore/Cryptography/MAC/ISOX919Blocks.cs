using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography.MAC
{
    public enum ISOX919Blocks
    {
        OnlyBlock = 0,
        FirstBlock = 1,
        NextBlock = 2,
        FinalBlock = 3
    }
}
