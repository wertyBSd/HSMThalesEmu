using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class SingleLengthDESCalculator_N : AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter data: ", "", new Validators.HexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter key: ", "", new Validators.HexKeyValidator()));
        }

        public override string ProcessMessage()
        {
            string data = m_inStack.PopFromStack().ConsoleMessageProperty;
            string desKey = m_inStack.PopFromStack().ConsoleMessageProperty;

            if (desKey.Length != 16)
                return "INVALID KEY";

            if (data.Length != 16)
                return "INVALID DATA";

            if (Utility.IsParityOK(desKey, Utility.ParityCheck.OddParity) == false)
                return "KEY PARITY ERROR";

            HexKey hk = new HexKey(desKey);
            string crypt = TripleDES.TripleDESEncrypt(hk, data);
            string decrypt = TripleDES.TripleDESDecrypt(hk, data);

            return "Encrypted: " + MakeKeyPresentable(crypt) + System.Environment.NewLine + "Decrypted: " + MakeKeyPresentable(decrypt);
        }
    }
}
