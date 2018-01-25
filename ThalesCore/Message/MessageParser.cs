using Message.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Message
{
    public class MessageParser
    {
        public static void Parse(Message msg, MessageFields fields, ref MessageKeyValuePairs KVPairs, out string result)
        {
            result = "";
            foreach (MessageField fld in fields.Fields)
            {
                fld.Skip = false;
            }

            int fldIdx = 0;

            while (fldIdx <= fields.Fields.Count - 1)
            {
                MessageField fld = fields.Fields[fldIdx];

                int repetitions = 1;
                if (fld.Repetitions != "")
                {
                    if (int.TryParse(fld.Repetitions, out repetitions))
                    {
                    }
                    else
                        repetitions = Convert.ToInt32(KVPairs.Item(fld.Repetitions));

                    if (fld.StaticRepetitions)
                    {
                        int nextNonStaticRepField = fldIdx + 1;
                        while ((nextNonStaticRepField <= fields.Fields.Count - 1) && (fields.Fields[nextNonStaticRepField].StaticRepetitions == true))
                            nextNonStaticRepField += 1;
                    }

                }
            }
            
        }
    }
}
