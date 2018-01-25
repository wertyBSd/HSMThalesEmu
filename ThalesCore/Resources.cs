using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class Resources
    {
        public const string FIRMWARE_NUMBER = "FIRMWARE_NUMBER";

        public const string DSP_FIRMWARE_NUMBER = "DSP_FIRMWARE_NUMBER";

        public const string MAX_CONS = "MAX_CONS";

        public const string AUTHORIZED_STATE = "AUTH_STATE";

        public const string LMK_CHECK_VALUE = "LMK_CHECK_VALUE";

        public const string CLEAR_PIN_LENGTH = "CLEAR_PIN_LENGTH";

        public const string WELL_KNOWN_PORT = "WELL_KNOWN_PORT";

        public const string CONSOLE_PORT = "CONSOLE_PORT";

        public const string HOST_COMMANDS_XML_DEFS = "HOST_COMMANDS_XML_DEFS";

        public const string DOUBLE_LENGTH_ZMKS = "DOUBLE_LENGTH_ZMKS";

        public const string LEGACY_MODE = "LEGACY_MODE";

        public const string EXPECT_TRAILERS = "EXPECT_TRAILERS";

        public const string HEADER_LENGTH = "HEADER_LENGTH";

        public const string EBCDIC = "USE_EBCDIC";

        private static SortedList<string, object> _lst = new SortedList<string, object>();

        public static void CleanUp()
        {
            _lst.Clear();
        }

        public static void AddResource(string key, object value)
        {
            _lst.Add(key, value);
        }

        public static object GetResource(string key)
        {
            return _lst[key];
        }

        public static void UpdateResource(string key, object value)
        {
            _lst.Remove(key);
            _lst.Add(key, value);
        }
    }
}
