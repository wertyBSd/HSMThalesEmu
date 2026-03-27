using System;
using System.Collections.Generic;
using System.Text;

public static class PayloadBuilder
{
    public static string BuildPayload(string actionKey, string param1, string param2, bool includeFlag)
    {
        if (string.IsNullOrEmpty(actionKey)) return string.Empty;

        // Map actions to request codes (must match MainForm)
        var actions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Echo", "B2" },
            { "Import Master Key", "A6" },
            { "Export Master Key (masked)", "A8" },
            { "Translate PIN (TPK->LMK)", "JC" },
            { "Verify PIN (IBM)", "DA" }
        };

        if (!actions.TryGetValue(actionKey, out var code)) return string.Empty;

        switch (actionKey)
        {
            case "Echo":
                {
                    string msg = param1 ?? string.Empty;
                    int len = Encoding.ASCII.GetByteCount(msg);
                    string lenHex = len.ToString("X4");
                    var payload = code + lenHex + msg;
                    if (includeFlag) payload += "F";
                    return payload;
                }
            case "Import Master Key":
                {
                    string keyType = (param1 ?? "000");
                    if (keyType.Length < 3) keyType = keyType.PadLeft(3, '0');
                    string keyHex = (param2 ?? string.Empty).ToUpper();
                    string zmkScheme = keyHex.Length >= 48 ? "T" : "U";
                    string zmk = zmkScheme + keyHex;
                    string keySchemeLMK = "U";
                    var payload = code + keyType + zmk + keyHex + keySchemeLMK;
                    if (includeFlag) payload += "F";
                    return payload;
                }
            case "Export Master Key (masked)":
                {
                    string keyType = (param1 ?? "000");
                    if (keyType.Length < 3) keyType = keyType.PadLeft(3, '0');
                    string keyHex = (param2 ?? string.Empty).ToUpper();
                    string zmkScheme = keyHex.Length >= 48 ? "T" : "U";
                    string zmk = zmkScheme + keyHex;
                    string keyScheme = "U";
                    var payload = code + keyType + zmk + keyHex + keyScheme;
                    if (includeFlag) payload += "F";
                    return payload;
                }
            case "Translate PIN (TPK->LMK)":
                {
                    string tpk = (param1 ?? string.Empty).ToUpper();
                    string pinblock = (param2 ?? string.Empty);
                    if (pinblock.Length == 4) pinblock = pinblock.PadRight(16, '0');
                    string format = "01";
                    string account = "000000000000";
                    var payload = code + tpk + pinblock + format + account;
                    if (includeFlag) payload += "F";
                    return payload;
                }
            case "Verify PIN (IBM)":
                {
                    string maxLen = "12";
                    string pinblock = (param1 ?? string.Empty);
                    if (pinblock.Length == 4) pinblock = pinblock.PadRight(16, '0');
                    string format = "01";
                    string checkLen = "04";
                    string account = (param2 ?? "000000000000").PadLeft(12, '0');
                    var payload = code + "" + "" + maxLen + pinblock + format + checkLen + account;
                    if (includeFlag) payload += "F";
                    return payload;
                }
            default:
                return string.Empty;
        }
    }
}
