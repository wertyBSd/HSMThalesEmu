using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class ImportKey_IK: AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter key: ", "", new Validators.FlexibleHexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter encrypted ZMK: ", "", new Validators.FlexibleHexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Scheme: ", "", new Validators.KeySchemeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Type: ", "", new ExtendedValidator(new Validators.AuthorizedStateValidator())
                                                                                 .AddLast(new Validators.KeyTypeValidator())));
        }

        public override string ProcessMessage()
        {
            LMKPairs.LMKPair LMKKeyPair; string var = "";
            KeySchemeTable.KeyScheme ks;
            HexKey.KeyLength kl;
            KeySchemeTable.KeyScheme zmkKS;
            HexKey.KeyLength zmkKL;

            string cryptKey = m_inStack.PopFromStack().ConsoleMessageProperty;
            string cryptZMK = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyScheme = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyType = m_inStack.PopFromStack().ConsoleMessageProperty;

            ExtractKeySchemeAndLength(cryptZMK, out zmkKL, out zmkKS);
            ExtractKeySchemeAndLength(cryptKey, out kl, out ks);
            ValidateKeyTypeCode(keyType, out LMKKeyPair, out var);

            if ((ks == KeySchemeTable.KeyScheme.DoubleLengthKeyVariant) || (ks == KeySchemeTable.KeyScheme.TripleLengthKeyVariant))
                return "INVALID KEY SCHEME FOR ENCRYPTED KEY - MUST BE ANSI";


            string clearZMK = Utility.DecryptZMKEncryptedUnderLMK(new HexKey(cryptZMK).ToString(), zmkKS, 0);
            string clearKey = TripleDES.TripleDESDecrypt(new HexKey(clearZMK), new HexKey(cryptKey).ToString());
            string cryptUnderLMK = Utility.EncryptUnderLMK(clearKey, KeySchemeTable.GetKeySchemeFromValue(keyScheme), LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearKey), ZEROES);
            return "Key under LMK: " + MakeKeyPresentable(cryptUnderLMK) + System.Environment.NewLine +
                   "Key Check Value: " + MakeCheckValuePresentable(chkVal);
        }
    }
}
