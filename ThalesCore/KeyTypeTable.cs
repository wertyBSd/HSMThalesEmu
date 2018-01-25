using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore
{
    public class KeyTypeTable
    {
        public enum KeyFunction
        {
            Generate = 0,
            Import = 1,
            Export = 2
        }

        public enum AuthorizedStateRequirement
        {
            NotAllowed = 0,
            NeedsAuthorizedState = 1,
            DoesNotNeedAuthorizedState = 2
        }

        private static AuthStateReqs[] Reqs =
            {
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair04_05, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair06_07, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair14_15, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair16_17, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair22_23, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair26_27, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair30_31, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair04_05, "1", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "1", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair04_05, "2", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "2", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "3", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair14_15, "4", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "4", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Generate, LMKPairs.LMKPair.Pair28_29, "5", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair06_07, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair14_15, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair16_17, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair22_23, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair26_27, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair30_31, "0", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair04_05, "1", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "1", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair04_05, "2", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "2", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "3", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair14_15, "4", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "4", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Import, LMKPairs.LMKPair.Pair28_29, "5", AuthorizedStateRequirement.DoesNotNeedAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair06_07, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair14_15, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair16_17, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair22_23, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair26_27, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair30_31, "0", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair04_05, "1", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "1", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair04_05, "2", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "2", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "3", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair14_15, "4", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "4", AuthorizedStateRequirement.NeedsAuthorizedState),
                                              new AuthStateReqs(KeyFunction.Export, LMKPairs.LMKPair.Pair28_29, "5", AuthorizedStateRequirement.NeedsAuthorizedState)
                    };

        public static void ParseKeyTypeCode(string keyTypeCode, out LMKPairs.LMKPair LMKKeyPair,out string Variant)
        {
            if ((keyTypeCode == null) || (keyTypeCode.Length != 3))
                throw new Exceptions.XInvalidKeyType("Invalid key type");
            string lmkpair;
            string var;

            var = keyTypeCode.Substring(0, 1);

            lmkpair = keyTypeCode.Substring(1, 2);

            try
            {
                if((Convert.ToInt32(var)<0)||(Convert.ToInt32(var)>9))
                    throw new Exceptions.XInvalidKeyType("Invalid Variant in key type (" + var + ")");
            }
            catch (Exception ex)
            {
                throw new Exceptions.XInvalidKeyType("Invalid Variant in key type (" + var + ")");
            }
            Variant = var;

            switch (lmkpair)
            {
                case "00":
                    LMKKeyPair = LMKPairs.LMKPair.Pair04_05;
                    break;
                case "01":
                    LMKKeyPair = LMKPairs.LMKPair.Pair06_07;
                    break;
                case "02":
                    LMKKeyPair = LMKPairs.LMKPair.Pair14_15;
                    break;
                case "03":
                    LMKKeyPair = LMKPairs.LMKPair.Pair16_17;
                    break;
                case "04":
                    LMKKeyPair = LMKPairs.LMKPair.Pair18_19;
                    break;
                case "05":
                    LMKKeyPair = LMKPairs.LMKPair.Pair20_21;
                    break;
                case "06":
                    LMKKeyPair = LMKPairs.LMKPair.Pair22_23;
                    break;
                case "07":
                    LMKKeyPair = LMKPairs.LMKPair.Pair24_25;
                    break;
                case "08":
                    LMKKeyPair = LMKPairs.LMKPair.Pair26_27;
                    break;
                case "09":
                    LMKKeyPair = LMKPairs.LMKPair.Pair28_29;
                    break;
                case "0A":
                    LMKKeyPair = LMKPairs.LMKPair.Pair30_31;
                    break;
                case "0B":
                    LMKKeyPair = LMKPairs.LMKPair.Pair32_33;
                    break;
                case "0C":
                    LMKKeyPair = LMKPairs.LMKPair.Pair34_35;
                    break;
                case "0D":
                    LMKKeyPair = LMKPairs.LMKPair.Pair36_37;
                    break;
                case "0E":
                    LMKKeyPair = LMKPairs.LMKPair.Pair38_39;
                    break;
                default:
                    throw new Exceptions.XInvalidKeyType("Invalid Variant in key type (" + var + ")");
            }

        }

        public static AuthorizedStateRequirement GetAuthorizedStateRequirement(KeyFunction NeededFunction, LMKPairs.LMKPair LMKKeyPair, string Variant)
        {
            for (int i = 0; i < Reqs.GetUpperBound(0); i++)
            {
                if ((Reqs[i].Func == NeededFunction) && (Reqs[i].LMKKeyPair == LMKKeyPair) && (Reqs[i].var == Variant))
                    return Reqs[i].Requirement;
            }

            return AuthorizedStateRequirement.NotAllowed;
        }

        private class AuthStateReqs
        {
            public KeyFunction Func;

            public LMKPairs.LMKPair LMKKeyPair;

            public string var;

            public AuthorizedStateRequirement Requirement;

            public AuthStateReqs(KeyFunction Func, LMKPairs.LMKPair LMKKeyPair, string Variant, AuthorizedStateRequirement Req)
            {
                this.Func = Func;
                this.LMKKeyPair = LMKKeyPair;
                this.var = Variant;
                this.Requirement = Req;
            }

        }
    }
}
