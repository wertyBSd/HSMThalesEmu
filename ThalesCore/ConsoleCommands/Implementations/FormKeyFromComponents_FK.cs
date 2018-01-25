using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Implementations
{
    public class FormKeyFromComponents_FK : AConsoleCommand
    {
        const string CLEAR_XOR_KEYS = "X";
        const string HALF_THIRD_KEYS = "H";
        const string ENCRYPTED_KEYS = "E";

        public override void InitializeStack()
        {
            m_stack.PushToStack(new ConsoleMessage("Enter", "", false, true, new Validators.FlexibleHexKeyValidator()));
            m_stack.PushToStack(new ConsoleMessage("Enter number of components (2-9): ", "", true, new Validators.NumberOfComponentsValidator()));
            m_stack.PushToStack(new ConsoleMessage("Component type [X,H,E,S]: ", "", new Validators.ComponentTypeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Scheme: ", "", new Validators.KeySchemeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key Type: ", "", new Validators.KeyTypeValidator()));
            m_stack.PushToStack(new ConsoleMessage("Key length [1,2,3]: ", "", new ExtendedValidator(new Validators.AuthorizedStateValidator())
                                                                                           .AddLast(new Validators.KeyLengthValidator())));
        }

        public override string ProcessMessage()
        {
            LMKPairs.LMKPair LMKKeyPair;
            string var = "";
            KeySchemeTable.KeyScheme ks;

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

            string compType = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyScheme = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyType = m_inStack.PopFromStack().ConsoleMessageProperty;
            string keyLen = m_inStack.PopFromStack().ConsoleMessageProperty;

            ValidateKeySchemeAndLength(keyLen, keyScheme, out ks);
            ValidateKeyTypeCode(keyType, out LMKKeyPair, out var);

            if (AllSameLength(components,idx) == false)
                throw new Exception("DATA INVALID; ALL KEYS MUST BE OF THE SAME LENGTH");

            if (compType == CLEAR_XOR_KEYS)
            {
                switch (ks)
                {
                    case KeySchemeTable.KeyScheme.SingleDESKey:
                        if (components[0].Length != 16)
                            throw new Exception("DATA INVALID; KEYS MUST BE 16 HEX CHARACTERS");
                        break;
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                        if (components[0].Length != 32)
                            throw new Exception("DATA INVALID; KEYS MUST BE 32 HEX CHARACTERS");
                        break;
                    case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                        if (components[0].Length != 48)
                            throw new Exception("DATA INVALID; KEYS MUST BE 48 HEX CHARACTERS");
                        break;
                }
            }
            if (compType == HALF_THIRD_KEYS)
            {
                switch (ks)
                {
                    case KeySchemeTable.KeyScheme.SingleDESKey:
                        if (components[0].Length != 8)
                            throw new Exception("DATA INVALID; SINGLE-LENGTH HALF-KEYS MUST BE 8 HEX CHARACTERS");
                        if(idx != 2)
                            throw new Exception("DATA INVALID; THERE MUST BE 2 HALF-KEYS");
                        break;
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                        if (components[0].Length != 16)
                            throw new Exception("DATA INVALID; DOUBLE-LENGTH HALF-KEYS MUST BE 16 HEX CHARACTERS");
                        if (idx != 2)
                            throw new Exception("DATA INVALID; THERE MUST BE 2 HALF-KEYS");
                        break;
                    case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                        if (components[0].Length != 16)
                            throw new Exception("DATA INVALID; TRIPLE-LENGTH THIRD-KEYS MUST BE 16 HEX CHARACTERS");
                        if (idx != 3)
                            throw new Exception("DATA INVALID; THERE MUST BE 3 THIRD-KEYS");
                        break;
                }
            }

            if (compType == ENCRYPTED_KEYS)
            {
                switch (ks)
                {
                    case KeySchemeTable.KeyScheme.SingleDESKey:
                        if (components[0].Length != 16)
                            throw new Exception("DATA INVALID; SINGLE-LENGTH ENCRYPTED COMPONENTS MUST BE 16 HEX CHARACTERS");
                        break;
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                        if (components[0].Length != 33)
                            throw new Exception("DATA INVALID; DOUBLE-LENGTH ENCRYPTED COMPONENTS MUST BE KEY SCHEME AND 32 HEX CHARACTERS");
                        
                        if(AllSameStartChar(components,idx) == false)
                            throw new Exception("DATA INVALID; DOUBLE-LENGTH ENCRYPTED COMPONENTS MUST ALL USE SAME KEY SCHEME");
                        break;
                    case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                    case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                        if (components[0].Length != 49)
                            throw new Exception("DATA INVALID; TRIPLE-LENGTH ENCRYPTED COMPONENTS MUST BE KEY SCHEME AND 48 HEX CHARACTERS");
                        if (AllSameStartChar(components,idx) == false)
                            throw new Exception("DATA INVALID; DOUBLE-LENGTH ENCRYPTED COMPONENTS MUST ALL USE SAME KEY SCHEME");
                        break;
                }
            }

            HexKey finalKey = null;
            switch (compType)
            {
                case HALF_THIRD_KEYS:
                    switch (ks)
                    {
                        case KeySchemeTable.KeyScheme.SingleDESKey:
                            finalKey = new HexKey(components[1] + components[0]);
                            break;
                        default:
                            string keyStr = "";
                            for (int i = components.GetUpperBound(0); i > -1; i++)
                                keyStr = keyStr + components[i];
                            finalKey = new HexKey(keyStr);
                            break;
                    }
                    break;
                case CLEAR_XOR_KEYS:
                    finalKey = new HexKey(XORAllKeys(components, idx));
                    break;
                case ENCRYPTED_KEYS:
                    string[] clearKeys = new string[idx - 1];
                    for (int i = 0; i < clearKeys.GetUpperBound(0); i++)
                        clearKeys[i] = Utility.DecryptUnderLMK(Utility.RemoveKeyType(components[i]), ks, LMKKeyPair, var);
                    finalKey = new HexKey(XORAllKeys(clearKeys, idx));
                    break;
            }
            finalKey = new HexKey(Utility.MakeParity(finalKey.ToString(), Utility.ParityCheck.OddParity));
            string cryptKey = Utility.EncryptUnderLMK(finalKey.ToString(), ks, LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(finalKey, ZEROES);

            return "Encrypted key: " + MakeKeyPresentable(cryptKey) + System.Environment.NewLine +
                   "Key check value: " + MakeCheckValuePresentable(chkVal);
        }

        private string XORAllKeys(string[] keys, int idx)
        {
            string xorred = keys[0];
            for (int i = 1; i < idx; i++)
                xorred = Utility.XORHexStringsFull(xorred, keys[i]);
            return xorred;
        }

        private bool AllSameStartChar(string[] keys, int idx)
        {
            string s = keys[0].Substring(0, 1);
            for (int i = 1; i < idx; i++)
                if (keys[i].Substring(0, 1) != s)
                    return false;
            return true;
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
