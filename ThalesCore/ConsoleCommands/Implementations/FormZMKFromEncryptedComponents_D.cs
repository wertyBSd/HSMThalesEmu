using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class FormZMKFromEncryptedComponents_D : AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter encrypted", "", false, true, new Validators.HexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter number of components (2-9): ", "", true, new ExtendedValidator(new Validators.NumberOfComponentsValidator())
                                                                                                               .AddLast(new Validators.AuthorizedStateValidator())));
        }

        public override string ProcessMessage()
        {
            string[] components = new string[8];
            int idx = 0;
            while (true)
            {
                ConsoleMessage msg = m_inStack.PopFromStack();
                if (msg.IsNumberOfComponents == false)
                {
                    components[idx] = msg.ConsoleMessageProperty;
                    idx += 1;
                }
                else
                    break;
            }

            if (AllSameLength(components, idx) == false)
                throw new Exception("DATA INVALID; ALL KEYS MUST BE OF THE SAME LENGTH");

            if(components[0].Length == 48)
                throw new Exception("TRIPLE LENGTH COMPONENT NOT SUPPORTED");

            string[] clearKeys= new string[idx];
            KeySchemeTable.KeyScheme ks = new HexKey(components[0]).Scheme;

            for (int i = 0; i < idx; i++)
                clearKeys[i] = Utility.DecryptZMKEncryptedUnderLMK(components[i], ks, 0);

            HexKey finalKey = new HexKey(XORAllKeys(clearKeys));

            finalKey = new HexKey(Utility.MakeParity(finalKey.ToString(), Utility.ParityCheck.OddParity));
            string cryptKey = Utility.EncryptUnderLMK(finalKey.ToString(), ks, LMKPairs.LMKPair.Pair04_05, "0");
            string chkVal = TripleDES.TripleDESEncrypt(finalKey, ZEROES);

            return "Encrypted key: " + MakeKeyPresentable(cryptKey) + System.Environment.NewLine +
                   "Key check value: " + MakeCheckValuePresentable(chkVal);
        }

        private string XORAllKeys(string[] keys)
        {
            string xorred = keys[0];
            for (int i = 1; i < keys.Length; i++)
                xorred = Utility.XORHexStringsFull(xorred, keys[i]);
            return xorred;
        }

        private bool AllSameLength(string[] keys, int idx)
        {
            int len = keys[0].Length;
            for (int i = 1; i < idx; i++)
                if (keys[i].Length != len)
                    return false;
            return true;
        }
    }
}
