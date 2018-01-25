using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class ExportKey_KE : AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter encrypted key: ", "", new Validators.FlexibleHexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter encrypted ZMK: ", "", new Validators.FlexibleHexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Scheme: ", "", new Validators.KeySchemeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Type: ", "", new ExtendedValidator(new Validators.AuthorizedStateValidator())
                                                                                 .AddLast(new Validators.KeyTypeValidator())));
        }

        public override string ProcessMessage()
        {
            LMKPairs.LMKPair LMKKeyPair = LMKPairs.LMKPair.Null;
            string var = "";
            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified;
            HexKey.KeyLength kl = HexKey.KeyLength.SingleLength;
            KeySchemeTable.KeyScheme zmkKS= KeySchemeTable.KeyScheme.Unspecified;
            HexKey.KeyLength zmkKL;

            string cryptKey = m_inStack.PopFromStack().ConsoleMessageProperty;
            string cryptZMK = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyScheme = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyType = m_inStack.PopFromStack().ConsoleMessageProperty;

            ExtractKeySchemeAndLength(cryptZMK, out zmkKL, out zmkKS);
            ExtractKeySchemeAndLength(cryptKey, out kl,out ks);
            ValidateKeyTypeCode(keyType, out LMKKeyPair, out var);

            string clearZMK = Utility.DecryptZMKEncryptedUnderLMK(new HexKey(cryptZMK).ToString(), zmkKS, 0);
            string clearKey = Utility.DecryptUnderLMK(new HexKey(cryptKey).ToString(), ks, LMKKeyPair, var);

            string cryptUnderZMK = Utility.EncryptUnderZMK(clearZMK, new HexKey(clearKey).ToString(), KeySchemeTable.GetKeySchemeFromValue(keyScheme));
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearKey), ZEROES);

            return "Key encrypted under ZMK: " + MakeKeyPresentable(cryptUnderZMK) + System.Environment.NewLine + 
                   "Key Check Value: " + MakeCheckValuePresentable(chkVal);
        }
    }
}
