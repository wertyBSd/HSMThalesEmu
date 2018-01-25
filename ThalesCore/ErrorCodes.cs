using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class ErrorCodes
    {
        public const string ER_00_NO_ERROR = "00";

        public const string ER_01_VERIFICATION_FAILURE = "01";
        
        public const string ER_02_INAPPROPRIATE_KEY_LENGTH_FOR_ALGORITHM = "02";

        public const string ER_03_INVALID_NUMBER_OF_COMPONENTS = "03";

        public const string ER_04_INVALID_KEY_TYPE_CODE = "04";

        public const string ER_05_INVALID_KEY_LENGTH_FLAG = "05";

        public const string ER_05_INVALID_HASH_IDENTIFIER = "05";

        public const string ER_10_SOURCE_KEY_PARITY_ERROR = "10";

        public const string ER_11_DESTINATION_KEY_PARITY_ERROR = "11";

        public const string ER_12_CONTENTS_OF_USER_STORAGE_NOT_AVAILABLE = "12";

        public const string ER_13_MASTER_KEY_PARITY_ERROR = "13";

        public const string ER_14_PIN_ENCRYPTED_UNDER_LMK_PAIR_02_03_IS_INVALID = "14";

        public const string ER_15_INVALID_INPUT_DATA = "15";

        public const string ER_16_CONSOLE_OR_PRINTER_NOT_READY_NOT_CONNECTED = "16";

        public const string ER_17_HSM_IS_NOT_IN_THE_AUTHORIZED_STATE = "17";

        public const string ER_18_DOCUMENT_DEFINITION_NOT_LOADED = "18";

        public const string ER_19_SPECIFIED_DIEBOLD_TABLE_IS_INVALID = "19";

        public const string ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES = "20";

        public const string ER_21_INVALID_INDEX_VALUE = "21";

        public const string ER_22_INVALID_ACCOUNT_NUMBER = "22";

        public const string ER_23_INVALID_PIN_BLOCK_FORMAT_CODE = "23";

        public const string ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG = "24";

        public const string ER_25_DECIMALIZATION_TABLE_ERROR = "25";

        public const string ER_26_INVALID_KEY_SCHEME = "26";

        public const string ER_27_INCOMPATIBLE_KEY_LENGTH = "27";

        public const string ER_28_INVALID_KEY_TYPE = "28";

        public const string ER_29_FUNCTION_NOT_PERMITTED = "29";

        public const string ER_30_INVALID_REFERENCE_NUMBER = "30";

        public const string ER_31_INSUFICCIENT_SOLICITATION_ENTRIES_FOR_BATCH = "31";

        public const string ER_33_LMK_KEY_CHANGE_STORAGE_IS_CORRUPTED = "33";

        public const string ER_40_INVALID_FIRMWARE_CHECKSUM = "40";

        public const string ER_41_INTERNAL_HARDWARE_SOFTWARE_ERROR = "41";

        public const string ER_42_DES_FAILURE = "42";

        public const string ER_51_INVALID_MESSAGE_HEADER = "51";

        public const string ER_52_INVALID_NUMBER_OF_COMMANDS = "52";

        public const string ER_80_DATA_LENGTH_ERROR = "80";

        public const string ER_90_DATA_PARITY_ERROR = "90";

        public const string ER_91_LRC_ERROR = "91";

        public const string ER_92_COUNT_VALUE_NOT_BETWEEN_LIMITS = "92";

        public const string ER_ZZ_UNKNOWN_ERROR = "ZZ";

        private static ThalesError[] _errors = {new ThalesError("00", "No error"),
                                               new ThalesError("01", "Verification failure"),
                                               new ThalesError("02", "Inappropriate key length for algorithm"),
                                               new ThalesError("03", "Invalid number of components"),
                                               new ThalesError("04", "Invalid key type code"),
                                               new ThalesError("05", "Invalid key length flag"),
                                               new ThalesError("10", "Source key parity error"),
                                               new ThalesError("11", "Destination key parity error"),
                                               new ThalesError("12", "Contents of user storage not available"),
                                               new ThalesError("13", "Master key parity error"),
                                               new ThalesError("14", "PIN encrypted under LMK pair 02-03 is invalid"),
                                               new ThalesError("15", "Invalid input data"),
                                               new ThalesError("16", "Console or printer not ready/not connected"),
                                               new ThalesError("17", "HSM is not in the authorized state"),
                                               new ThalesError("18", "Document definition not loaded"),
                                               new ThalesError("19", "Specified Diebold table is invalid"),
                                               new ThalesError("20", "PIN block does not contain valid values"),
                                               new ThalesError("21", "Invalid index value"),
                                               new ThalesError("22", "Invalid account number"),
                                               new ThalesError("23", "Invalid PIN block format code"),
                                               new ThalesError("24", "PIN is fewer than 4 or more than 12 digits long"),
                                               new ThalesError("25", "Decimalization table error"),
                                               new ThalesError("26", "Invalid key scheme"), 
                                               new ThalesError("27", "Incompatible key length"),
                                               new ThalesError("28", "Invalid key type"), 
                                               new ThalesError("29", "Function not permitted"),
                                               new ThalesError("30", "Invalid reference number"),
                                               new ThalesError("31", "Insuficcient solicitation entries for batch"),
                                               new ThalesError("33", "LMK key change storage is corrupted"),
                                               new ThalesError("40", "Invalid firmware checksum"),
                                               new ThalesError("41", "Internal hardware/software error"),
                                               new ThalesError("42", "DES failure"),
                                               new ThalesError("51", "Invalid message header"),
                                               new ThalesError("52", "Invalid number of command fields"),
                                               new ThalesError("80", "Data length error"),
                                               new ThalesError("90", "Data parity error"),
                                               new ThalesError("91", "LRC error"),
                                               new ThalesError("92", "Count value not between limits"),
                                               new ThalesError("ZZ", "UNKNOWN ERROR")};
        public static ThalesError GetError(string errorCode)
        {
            for (int i = 0; i < _errors.GetUpperBound(0); i++)
            {
                if (_errors[i].ErrorCode == errorCode) return _errors[i];
            }
            return null;
        }
    }
}
