using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class EncryptClearComponent_EC : AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter component: ", "", new Validators.HexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Scheme: ", "", new Validators.KeySchemeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Type: ", "", new ExtendedValidator(new Validators.AuthorizedStateValidator()).AddLast(new Validators.KeyTypeValidator())));

        }

        public override string ProcessMessage()
        {
            LMKPairs.LMKPair LMKKeyPair;
            string var = "";
            KeySchemeTable.KeyScheme ks;

            string clearComponent = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyScheme = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyType = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyLen;

            switch (clearComponent.Length)
            {
                case 16:
                    keyLen = "1";
                    break;
                case 32:
                    keyLen = "2";
                    break;
                default:
                    keyLen = "3";
                    break;
            }
            ValidateKeySchemeAndLength(keyLen, keyScheme, out ks);
            ValidateKeyTypeCode(keyType, out LMKKeyPair, out var);

            clearComponent = Utility.MakeParity(clearComponent, Utility.ParityCheck.OddParity);

            string cryptKey = Utility.EncryptUnderLMK(clearComponent, ks, LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearComponent), ZEROES);

            return "Encrypted Component: " + MakeKeyPresentable(cryptKey) + System.Environment.NewLine +
                   "Key check value: " + MakeCheckValuePresentable(chkVal);
        }
    }
}
