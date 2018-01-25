using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography
{
    public class DES
    {
        public static void byteDESEncrypt(byte[] bKey, byte[] bData, out byte[] bResult)
        {
            try
            {
                MemoryStream outStream = new MemoryStream();
                DESCryptoServiceProvider desProvider = new DESCryptoServiceProvider();
                CryptoStream csMyCryptoStream;
                byte[] bNullVector = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

                bResult = new byte[8];

                if (DESCryptoServiceProvider.IsWeakKey(bKey))
                {
                    Log.Logger.MajorWarning("***DES Encrypt with weak key***");
                    for (int i = 0; i < 8; i++)
                    {
                        bResult[i] = bData[i];
                    }
                    return;
                }

                outStream = new MemoryStream(bResult);

                desProvider.Mode = CipherMode.ECB;
                desProvider.Key = bKey;
                desProvider.IV = bNullVector;
                desProvider.Padding = PaddingMode.None;

                csMyCryptoStream = new CryptoStream(outStream, desProvider.CreateEncryptor(bKey, bNullVector), CryptoStreamMode.Write);
                csMyCryptoStream.Write(bData, 0, 8);
                csMyCryptoStream.Close();
            }
            catch (Exception ex)
            {
                throw new Exceptions.XEncryptError(ex.Message);
            }
        }

        public static void byteDESDecrypt(byte[] bKey, byte[] bData, out byte[] bResult)
        {
            try
            {
                MemoryStream outStream = new MemoryStream();
                DESCryptoServiceProvider desProvider = new DESCryptoServiceProvider();
                CryptoStream csMyCryptoStream;
                byte[] bNullVector = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

                bResult = new byte[8];

                if (DESCryptoServiceProvider.IsWeakKey(bKey))
                {
                    Log.Logger.MajorWarning("***DES Decrypt with weak key***");
                    for (int i = 0; i < 8; i++)
                    {
                        bResult[i] = bData[i];
                    }
                    return;
                }

                desProvider.Mode = CipherMode.ECB;
                desProvider.Key = bKey;
                desProvider.IV = bNullVector;
                desProvider.Padding = PaddingMode.None;

                outStream = new MemoryStream(bResult);
                csMyCryptoStream = new CryptoStream(outStream, desProvider.CreateDecryptor(bKey, bNullVector), CryptoStreamMode.Write);
                csMyCryptoStream.Write(bData, 0, 8);
                csMyCryptoStream.Close();
            }
            catch (Exception ex)
            {
                throw new Exceptions.XDecryptError(ex.Message);
            }
        }

        public static string DESEncrypt(string sKey, string sData)
        {
            byte[] bKey = new byte[8];
            byte[] bData = new byte[8];
            byte[] bOutput = new byte[8];
            string sResult = String.Empty;
            try
            {
                Utility.HexStringToByteArray(sKey, bKey);
                Utility.HexStringToByteArray(sData, bData);
                byteDESEncrypt(bKey, bData, out bOutput);
                Utility.ByteArrayToHexString(bOutput, out sResult);
                return sResult;
            }
            catch (Exception ex)
            {
                throw new Exceptions.XEncryptError(ex.Message);
            }
        }

        public static string DESDecrypt(string sKey, string sData)
        {
            byte[] bKey = new byte[8];
            byte[] bData = new byte[8];
            byte[] bOutput = new byte[8];
            string sResult = String.Empty;
            try
            {
                Utility.HexStringToByteArray(sKey, bKey);
                Utility.HexStringToByteArray(sData, bData);
                byteDESDecrypt(bKey, bData, out bOutput);
                Utility.ByteArrayToHexString(bOutput, out sResult);
                return sResult;
            }
            catch (Exception ex)
            {
                throw new Exceptions.XDecryptError(ex.Message);
            }
        }

    }
}
