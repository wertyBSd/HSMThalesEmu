using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Cryptography.LMK
{
    public class Variants
    {
        private static string[] _Variants = { "A6", "5A", "6A", "DE", "2B", "50", "74", "9C", "FA" };

        private static string[] _doubleKeyVariants = { "A6", "5A" };

        private static string[] _tripleKeyVariants = { "6A", "DE", "2B" };

        public static string VariantNbr(int index)
        {
            return _Variants[index - 1];
        }

        public static string DoubleLengthVariant(int index)
        {
            return _doubleKeyVariants[index - 1];
        }

        public static string TripleLengthVariant(int index)
        {
            return _tripleKeyVariants[index - 1];
        }

    }
}
