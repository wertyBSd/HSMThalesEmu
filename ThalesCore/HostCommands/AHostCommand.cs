using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore;
using ThalesCore.Cryptography;
using ThalesCore.Message;

namespace HostCommands
{
    public class AHostCommand
    {
        protected string m_PrinterData = "";

        protected Message.XML.MessageFields m_msgFields= new Message.XML.MessageFields();

        protected string m_XMLParseResult = ErrorCodes.ER_00_NO_ERROR;

        protected Message.XML.MessageKeyValuePairs kvp = new Message.XML.MessageKeyValuePairs();

        public Message.XML.MessageFields XMLMessageFields
        {
            get { return m_msgFields; }
            set {  m_msgFields = value; }
        }

        public string XMLParseResult
        {
            get { return m_XMLParseResult; }
            set { m_XMLParseResult = value; }
        }

        public Message.XML.MessageKeyValuePairs KeyValuePairs
        {
            get { return kvp; }
        }

        public string PrinterData
        {
            get { return m_PrinterData; }
        }

        public virtual void AcceptMessage(ThalesCore.Message.Message msg)
        {

        }

        public virtual MessageResponse ConstructResponse()
        {
            return null;
        }

        public MessageResponse ConstructResponseAfterOperationComplete()
        {
            return null;
        }

        public void Terminate()
        {

        }

        public string DumpFields()
        {
            return kvp.ToString();
        }

        protected bool ValidateKeyTypeCode(string ktc, out LMKPairs.LMKPair Pair, ref string Var, ref ThalesCore.Message.MessageResponse MR)
        {
            Pair = LMKPairs.LMKPair.Null;
            try
            {
                KeyTypeTable.ParseKeyTypeCode(ktc, out Pair, out Var);
                return true;
            }
            catch (ThalesCore.Exceptions.XInvalidKeyType ex)
            {
                MR.AddElement(ErrorCodes.ER_04_INVALID_KEY_TYPE_CODE);
                return false;
            }
        }

        protected bool ValidateKeySchemeCode(string ksc, ref KeySchemeTable.KeyScheme KS, ref ThalesCore.Message.MessageResponse MR)
        {
            try
            {
                KS = KeySchemeTable.GetKeySchemeFromValue(ksc);
                return true;
            }
            catch (ThalesCore.Exceptions.XInvalidKeyType ex)
            {
                MR.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                return false;
            }
        }

        protected bool ValidateFunctionRequirement(KeyTypeTable.KeyFunction func, LMKPairs.LMKPair Pair, string var, ThalesCore.Message.MessageResponse MR)
        {
            KeyTypeTable.AuthorizedStateRequirement requirement = KeyTypeTable.GetAuthorizedStateRequirement(KeyTypeTable.KeyFunction.Generate, Pair, var);
            if (requirement == KeyTypeTable.AuthorizedStateRequirement.NotAllowed)
            {
                MR.AddElement(ErrorCodes.ER_29_FUNCTION_NOT_PERMITTED);
                return false;
            }
            else if ((requirement == KeyTypeTable.AuthorizedStateRequirement.NeedsAuthorizedState) && (Convert.ToBoolean(Resources.GetResource(Resources.AUTHORIZED_STATE)) == false))
            {
                MR.AddElement(ErrorCodes.ER_17_HSM_IS_NOT_IN_THE_AUTHORIZED_STATE);
                return false;
            }
            else
                return true;
        }

        protected bool IsInAuthorizedState()
        {
            return Convert.ToBoolean(Resources.GetResource(Resources.AUTHORIZED_STATE));
        }

        protected bool IsInLegacyMode()
        {
            return Convert.ToBoolean(Resources.GetResource(Resources.LEGACY_MODE));
        }

        protected string DecryptUnderZMK(string clearZMK, string cryptData, KeySchemeTable.KeyScheme ZMK_KeyScheme)
        {
            return DecryptUnderZMK(clearZMK, cryptData, ZMK_KeyScheme, String.Empty);
        }

        protected string DecryptUnderZMK(string clearZMK, string cryptData, KeySchemeTable.KeyScheme ZMK_KeyScheme, string AtallaVariant)
        {
            string result = "";
            clearZMK = Utility.TransformUsingAtallaVariant(clearZMK, AtallaVariant);

            switch (ZMK_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.SingleDESKey:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.Unspecified:
                    result = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(clearZMK), cryptData);
                    break;
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = ThalesCore.Cryptography.TripleDES.TripleDESDecryptVariant(new ThalesCore.Cryptography.HexKey(clearZMK), cryptData);
                    break;
            }
            switch (ZMK_KeyScheme)
            {
                case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant:
                case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi:
                case KeySchemeTable.KeyScheme.TripleLengthKeyVariant:
                    result = KeySchemeTable.GetKeySchemeValue(ZMK_KeyScheme) + result;
                    break;
            }
            return result;
        }

        protected string GetRandomPIN(int pinLength)
        {
            Random rndMachine = new Random();
            string PIN = "";
            for (int i = 0; i < pinLength; i++)
                PIN += Convert.ToString(rndMachine.Next(0, 10));
            rndMachine = null;
            return PIN;
        }

        protected string EncryptPINForHostStorage(string PIN)
        {
            return "0" + PIN;
        }

        protected string DecryptPINUnderHostStorage(string PIN)
        {
            return PIN.Substring(1);
        }

        protected string EncryptPINForHostStorageThales(string PIN)
        {
            return "0" + PIN;
        }

        protected string DecryptPINUnderHostStorageRacal(string PIN)
        {
            return PIN.Substring(1);
        }


        protected string GeneratePVV(string AccountNumber, string PVKI, string PIN, string PVKPair)
        {
            string stage1 = AccountNumber.Substring(1, 11) + PVKI + PIN.Substring(0, 4);
            string stage2 = TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(PVKPair), stage1);
            string PVV = ""; int i;

            while (PVV.Length < 4)
            {
                i = 0;
                while ((PVV.Length < 4) && (i < stage2.Length))
                {
                    if (Char.IsDigit(stage2[i]))
                    {
                        PVV += stage2.Substring(i, 1);
                        i += 1;
                    }
                }
                if (PVV.Length < 4)
                {
                    for (int j = 0; j < stage2.Length; j++)
                    {
                        string newChar = " ";
                        if (Char.IsDigit(stage2[j]) == false)
                            newChar = (Convert.ToInt32(stage2.Substring(j, 1), 16) - 10).ToString("X");

                        stage2 = stage2.Remove(j, 1);
                        stage2 = stage2.Insert(j, newChar);
                    }

                    stage2 = stage2.Replace(" ", "");
                }

            }

            return PVV;
        }

        protected string GenerateCVV(string CVKPair, string AccountNumber, string ExpirationDate, string SVC)
        {
            string CVKA = Utility.RemoveKeyType(CVKPair).Substring(0, 16);
            string CVKB = Utility.RemoveKeyType(CVKPair).Substring(16);
            string block = (AccountNumber + ExpirationDate + SVC).PadRight(32, '0');
            string blockA = block.Substring(0, 16);
            string blockB = block.Substring(16);

            string result  = TripleDES.TripleDESEncrypt(new HexKey(CVKA), blockA);
            result = Utility.XORHexStrings(result, blockB);
            result = TripleDES.TripleDESEncrypt(new HexKey(CVKA + CVKB), result);

            string CVV = ""; int i = 0;

            while (CVV.Length < 3)
            {
                if (Char.IsDigit(result[i]))
                    CVV += result.Substring(i, 1);
                i += 1;
            }
            return CVV;
        }

        protected string GenerateMAC(byte[] b, string key, string IV)
        {
            int curIndex = 0;
            string result = "";
            while (curIndex <= b.GetUpperBound(0))
            {
                MACBytes(b, curIndex, IV, key, result);
                IV = result;
            }
            return result;
        }

        private void MACBytes(byte[] b, int curIndex, string IV, string key, string result)
        {
            string dataStr = "";
            while (dataStr.Length != 16)
            {
                if (curIndex <= b.GetUpperBound(0))
                {
                    dataStr = dataStr + b[curIndex].ToString("X2");
                    curIndex += 1;
                }
                else
                    dataStr = dataStr.PadRight(16, '0');
            }
            dataStr = Utility.XORHexStringsFull(dataStr, IV);
            result = TripleDES.TripleDESEncrypt(new HexKey(key), dataStr);
        }

        protected void AddPrinterData(string data)
        {
            m_PrinterData = PrinterData + data + System.Environment.NewLine;
        }

        protected void ClearPrinterData()
        {
            m_PrinterData = "";
        }

        protected void ReadXMLDefinitions()
        {
            ReadXMLDefinitions(this.GetType().Name + ".xml");
        }

        protected void ReadXMLDefinitions(bool forceRead)
        {
            ReadXMLDefinitions(forceRead, this.GetType().Name + ".xml");
        }

        protected void ReadXMLDefinitions(string fileName)
        {
            ReadXMLDefinitions(false, fileName);
        }

        protected void ReadXMLDefinitions(bool forceRead, string fileName)
        {
            if (forceRead)
                ThalesCore.Message.XML.MessageFieldsStore.Remove(this.GetType().Name);

            XMLMessageFields = ThalesCore.Message.XML.MessageFieldsStore.Item(this.GetType().Name);

            if (XMLMessageFields == null)
            {
                XMLMessageFields = Message.XML.MessageFields.ReadXMLFields(fileName);
                ThalesCore.Message.XML.MessageFieldsStore.Add(this.GetType().Name, XMLMessageFields);
            }

        }

    }
}
