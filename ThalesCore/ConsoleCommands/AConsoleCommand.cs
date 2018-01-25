using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands
{
    public class AConsoleCommand
    {
        protected const string ZEROES = "0000000000000000";

        protected bool m_commandFinished = false;

        protected ConsoleMessageStack m_stack = new ConsoleMessageStack();

        protected ConsoleMessageStack m_inStack = new ConsoleMessageStack();

        protected ConsoleMessage m_curMessage = null;

        protected bool hasComponents = false;

        protected int numComponents;

        public bool CommandFinished
        {
            get { return (m_stack.MessagesOnStack() == 0); }
        }

        public bool IsNoinputCommand()
        {
            return (m_stack.MessagesOnStack() == 0);
        }

        public string GetClientMessage()
        {
            m_curMessage = m_stack.PopFromStack();

            if (hasComponents)
            {
                if (m_curMessage.IsComponent)
                {
                    for (int i = numComponents - 2; i > -1; i--)
                    {
                        ConsoleMessage newMsg = new ConsoleMessage(m_curMessage.ClientMessage + " component #" + (i + 2).ToString() + ": ", m_curMessage.ConsoleMessageProperty, false, false, m_curMessage.ConsoleMessageValidator);
                        m_stack.PushToStack(newMsg);
                    }
                    return m_curMessage.ClientMessage + " component #1: ";
                }
            }
            return m_curMessage.ClientMessage;
        }

        public string AcceptMessage(string consoleMsg)
        {
            m_curMessage.ConsoleMessageProperty = consoleMsg;

            if (m_curMessage.ConsoleMessageValidator != null)
            {
                try
                {
                    m_curMessage.ConsoleMessageValidator.ValidateConsoleMessage(m_curMessage.ConsoleMessageProperty);
                }
                catch (Exception ex)
                {
                    while (m_stack.MessagesOnStack() != 0)
                    {
                        m_stack.PopFromStack();
                    }
                    return ex.Message;
                }
            }
            if (m_curMessage.IsNumberOfComponents)
            {
                hasComponents = true;
                numComponents = Convert.ToInt32(m_curMessage.ConsoleMessageProperty);
            }

            m_inStack.PushToStack(m_curMessage);

            if (m_stack.MessagesOnStack() == 0)
            {
                return ProcessMessage();
            }
            else
                return null;
        }

        public virtual void InitializeStack()
        { }

        public virtual string ProcessMessage()
        {
            return null;
        }

        protected void ValidateKeyTypeCode(string ktc, out LMKPairs.LMKPair Pair, out string Var)
        {
            KeyTypeTable.ParseKeyTypeCode(ktc, out Pair, out Var);
        }

        protected void ValidateKeySchemeCode(string ksc, KeySchemeTable.KeyScheme KS)
        {
            KS = KeySchemeTable.GetKeySchemeFromValue(ksc);
        }

        protected void ValidateKeySchemeAndLength(string keyLen, string keyScheme,out KeySchemeTable.KeyScheme ks)
        {
            switch (keyLen)
            {
                case "1":
                    ks = KeySchemeTable.KeyScheme.SingleDESKey;
                    if (keyScheme != "0")
                        throw new Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH");
                    break;
                case "2":
                    switch (keyScheme)
                    {
                        case "U":
                            ks = KeySchemeTable.KeyScheme.DoubleLengthKeyVariant;
                            break;
                        case "X":
                            ks = KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi;
                            break;
                        default:
                            throw new Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH");
                    }
                    break;
                case "3":
                    switch (keyScheme)
                    {
                        case "Y":
                            ks = KeySchemeTable.KeyScheme.TripleLengthKeyAnsi;
                            break;
                        case "T":
                            ks = KeySchemeTable.KeyScheme.TripleLengthKeyVariant;
                            break;
                        default:
                            throw new Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH");
                    }
                    break;
                default:
                    throw new Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH");
            }
        }


        protected void ValidateKeySchemeAndLength(HexKey.KeyLength keyLen, string keyScheme, KeySchemeTable.KeyScheme ks)
        {
            switch (keyLen)
            {
                case HexKey.KeyLength.SingleLength:
                    ValidateKeySchemeAndLength("1", keyScheme,out ks);
                    break;
                case HexKey.KeyLength.DoubleLength:
                    ValidateKeySchemeAndLength("2", keyScheme,out ks);
                    break;
                case HexKey.KeyLength.TripleLength:
                    ValidateKeySchemeAndLength("3", keyScheme,out ks);
                    break;
                default:
                    throw new Exceptions.XInvalidKeyScheme("INVALID KEY SCHEME FOR KEY LENGTH");
            }
        }

        protected void ValidateKeySchemeAndLength(KeyTypeTable.KeyFunction func, LMKPairs.LMKPair Pair, string var)
        {
            KeyTypeTable.AuthorizedStateRequirement requirement = KeyTypeTable.GetAuthorizedStateRequirement(KeyTypeTable.KeyFunction.Generate, Pair, var);
            if (requirement == KeyTypeTable.AuthorizedStateRequirement.NotAllowed)
                throw new Exceptions.XFunctionNotPermitted("FUNCTION NOT PERMITTED");
            else if ((requirement == KeyTypeTable.AuthorizedStateRequirement.NeedsAuthorizedState) && (Convert.ToBoolean(Resources.GetResource(Resources.AUTHORIZED_STATE)) == false))
                throw new Exceptions.XNeedsAuthorizedState("NOT AUTHORIZED");
        }

        protected void ExtractKeySchemeAndLength(string key, out Cryptography.HexKey.KeyLength keyLen, out KeySchemeTable.KeyScheme keyScheme)
        {
            HexKey hk = new HexKey(key);
            keyLen = hk.KeyLen;
            keyScheme = hk.Scheme;
        }

        protected string MakeKeyPresentable(string key)
        {
            string ret = "";
            int inIdx = 0;
            if (key.Length % 16 != 0)
            {
                ret = key.Substring(0, 1) + " ";
                inIdx = 1;
            }

            while (inIdx < key.Length)
            {
                ret = ret + key.Substring(inIdx, 4) + " ";
                inIdx += 4;
            }
            return ret;
        }

        protected string MakeCheckValuePresentable(string cv)
        {
            return cv.Substring(0, 4) + " " + cv.Substring(4, 2);
        }
    }
}
