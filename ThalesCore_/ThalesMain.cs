using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThalesCore.Log;

namespace ThalesCore
{
    public class ThalesMain: ILogProcs
    {
        private int port;
        private int consolePort;
        private int maxCons;
        private int curCons = 0;
        private int consoleCurCons  = 0;
        private string LMKFile;
        private string VBsources;
        private bool CheckLMKParity;
        private string HostDefsDir;
        private bool DoubleLengthZMKs;
        private bool LegacyMode;
        private bool ExpectTrailers;
        private int HeaderLength;
        private bool EBCDIC;

        Thread LT;
        Thread CLT;

        void ILogProcs.GetMajor(string s)
        {
            throw new NotImplementedException();
        }

        void ILogProcs.GetMinor(string s)
        {
            throw new NotImplementedException();
        }
    }
}
