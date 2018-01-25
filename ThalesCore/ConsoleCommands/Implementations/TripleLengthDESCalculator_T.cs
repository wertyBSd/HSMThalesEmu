using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class TripleLengthDESCalculator_T : AConsoleCommand
    {
        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter data: ", "", new Validators.HexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Single, Double or Triple length data (S,D,T): ", "", new Validators.DataLengthValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter key: ", "", new Validators.HexKeyValidator()));
        }

        public override string ProcessMessage()
        {
            string data = m_inStack.PopFromStack().ConsoleMessageProperty;
            string length = m_inStack.PopFromStack().ConsoleMessageProperty;
            string desKey = m_inStack.PopFromStack().ConsoleMessageProperty;

            if (desKey.Length != 48)
                return "INVALID KEY";

            if (Utility.IsParityOK(desKey, Utility.ParityCheck.OddParity) == false)
                return "KEY PARITY ERROR";

            if (((data.Length == 16) && (length != "S")) ||
             ((data.Length == 32) && (length != "D")) ||
            ((data.Length == 48) && (length != "T")))
                return "INVALID DATA LENGTH";

            HexKey hk = new HexKey(desKey);
            string crypt = TripleDES.TripleDESEncrypt(hk, data);
            string decrypt = TripleDES.TripleDESDecrypt(hk, data);

            return "Encrypted: " + MakeKeyPresentable(crypt) + System.Environment.NewLine + "Decrypted: " + MakeKeyPresentable(decrypt);
        }
    }
}
