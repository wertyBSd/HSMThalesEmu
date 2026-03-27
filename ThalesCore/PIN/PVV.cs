using System;
using System.Linq;
using System.Security.Cryptography;

namespace ThalesCore.PIN
{
    public static class PVV
    {
        private static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            hex = hex.Replace(" ", "");
            if (hex.Length % 2 != 0) hex = "0" + hex;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        private static byte[] Make3DesKey(byte[] key)
        {
            if (key == null) return Array.Empty<byte>();
            if (key.Length == 24) return key;
            if (key.Length == 16) return key; // two-key 3DES (16 bytes)
            if (key.Length == 8) // single DES -> make two-key 3DES by repeating
            {
                var k = new byte[16];
                Array.Copy(key, 0, k, 0, 8);
                Array.Copy(key, 0, k, 8, 8);
                return k;
            }
            // normalize: if odd or other length, pad/truncate to 16
            var outk = new byte[16];
            for (int i = 0; i < outk.Length; i++) outk[i] = i < key.Length ? key[i] : (byte)0x00;
            return outk;
        }

        private static byte[] BcdFromDigits(string digits)
        {
            if (digits.Length % 2 != 0) digits = "0" + digits;
            var outBytes = new byte[digits.Length / 2];
            for (int i = 0; i < outBytes.Length; i++)
            {
                var hi = (byte)(digits[i * 2] - '0');
                var lo = (byte)(digits[i * 2 + 1] - '0');
                outBytes[i] = (byte)((hi << 4) | lo);
            }
            return outBytes;
        }

        private static byte[] Encrypt3DesEcb(byte[] key, byte[] block)
        {
            using var tdes = System.Security.Cryptography.TripleDES.Create();
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.None;
            try
            {
                tdes.Key = key;
            }
            catch (CryptographicException)
            {
                // Some keys are flagged as weak by the platform; adjust the last byte slightly
                // and use the adjusted key so encryption can proceed for testing/emulation.
                var alt = (byte[])key.Clone();
                alt[alt.Length - 1] ^= 0xF0;
                tdes.Key = alt;
            }
            using var enc = tdes.CreateEncryptor();
            return enc.TransformFinalBlock(block, 0, block.Length);
        }

        private static byte[] Encrypt3DesEcbUsingProject(string keyHex, byte[] block)
        {
            var dataHex = BitConverter.ToString(block).Replace("-", "");
            var hexKey = new ThalesCore.Cryptography.HexKey(keyHex);
            var outHex = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(hexKey, dataHex);
            var outBytes = new byte[outHex.Length / 2];
            for (int i = 0; i < outBytes.Length; i++)
                outBytes[i] = Convert.ToByte(outHex.Substring(i * 2, 2), 16);
            return outBytes;
        }

        // Compute a Visa-style PVV (4 digits) using a 3DES key and PAN.
        // This implementation uses a common decimalization approach: encrypt a 16-digit numeric
        // block (12 right-most PAN digits excluding check digit + 4 zeros), decimalize nibbles via nibble%10,
        // and take the first 4 resulting digits.
        public static string ComputeVisaPVV(string keyHex, string pan)
        {
            // Use the project's TripleDES helper which operates on hex keys
            var keyBytes = HexToBytes(keyHex);
            var tdesKey = Make3DesKey(keyBytes); 

            // derive 12 right-most PAN digits excluding the check digit (if available)
            string pan12;
            if (string.IsNullOrEmpty(pan)) pan12 = new string('0', 12);
            else if (pan.Length >= 13) pan12 = pan.Substring(pan.Length - 13, 12);
            else pan12 = pan.PadLeft(12, '0');

            var inputDigits = pan12 + "0000"; // 16 digits -> 8 BCD bytes
            var block = BcdFromDigits(inputDigits);
            var enc = Encrypt3DesEcbUsingProject(keyHex, block);

            // decimalize each nibble as nibble % 10
            var digits = new char[enc.Length * 2];
            for (int i = 0; i < enc.Length; i++)
            {
                int hi = (enc[i] >> 4) & 0xF;
                int lo = enc[i] & 0xF;
                digits[i * 2] = (char)('0' + (hi % 10));
                digits[i * 2 + 1] = (char)('0' + (lo % 10));
            }

            return new string(digits).Substring(0, 4);
        }

        // Compute IBM 3624 offset for a given PIN by deriving a natural PIN from the same decimalization
        // process and returning the per-digit offset such that (natural + offset) mod 10 = PIN.
        public static string ComputeIBM3624Offset(string keyHex, string pan, string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length < 4) throw new ArgumentException("PIN must be at least 4 digits", nameof(pin));
            var natural = ComputeVisaPVV(keyHex, pan); // reuse same decimalization to derive a natural value
            // use first 4 digits of natural for offset computation
            var refDigits = natural.Substring(0, 4);
            var sb = new char[4];
            for (int i = 0; i < 4; i++)
            {
                int pd = pin[i] - '0';
                int rd = refDigits[i] - '0';
                int offset = (pd - rd + 10) % 10;
                sb[i] = (char)('0' + offset);
            }
            return new string(sb);
        }
    }
}
