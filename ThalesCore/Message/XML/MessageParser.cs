using Message.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Message.XML
{
    public class MessageParser
    {
        public static void Parse(Message msg, MessageFields fields,
                                ref MessageKeyValuePairs KVPairs, out string result)
        {
            foreach (MessageField fld in fields.Fields)
            {
                fld.Skip = false;
            }
            int fldIdx = 0;
            while (fldIdx <= fields.Fields.Count - 1)
            {
                MessageField fld = fields.Fields[fldIdx];

                int repetitions = 1;
                if (!String.IsNullOrEmpty(fld.Repetitions))
                {
                    int Num;
                    bool isNum = int.TryParse(fld.Repetitions, out Num);
                    if (isNum) repetitions = Convert.ToInt32(fld.Repetitions);
                    else repetitions = Convert.ToInt32(KVPairs.Item(fld.Repetitions));

                    if (fld.StaticRepetitions)
                    {
                        int nextNonStaticRepField = fldIdx + 1;
                        while (nextNonStaticRepField <= fields.Fields.Count - 1 && fields.Fields[nextNonStaticRepField].StaticRepetitions)
                        {
                            nextNonStaticRepField++;
                        }

                        List<MessageField> dynamicFields = new List<MessageField>();
                        for (int i = fldIdx; i < nextNonStaticRepField; i++)
                        {
                            dynamicFields.Add(fields.Fields[i]);
                        }

                        for (int i = fldIdx; i < nextNonStaticRepField; i++)
                        {
                            fields.Fields.RemoveAt(fldIdx);
                        }

                        int insertPos = fldIdx;
                        List<string> fieldList = new List<string>();
                        for (int i = 1; i < repetitions; i++)
                        {
                            for (int j = 0; j < dynamicFields.Count - 1; j++)
                            {
                                MessageField newFld = dynamicFields[j].Clone();
                                newFld.Repetitions = "";
                                newFld.StaticRepetitions = false;
                                if (!fieldList.Contains(newFld.Name))
                                {
                                    fieldList.Add(newFld.Name);
                                }

                                //Save the ORIGINAL field name.
                                newFld.Name = newFld.Name + " #" + i.ToString();


                                if (fieldList.Contains(newFld.DependentField))
                                {
                                    newFld.DependentField = newFld.DependentField + " #" + i.ToString();
                                }

                                if (fieldList.Contains(newFld.DynamicLength))
                                {
                                    newFld.DynamicLength = newFld.DynamicLength + " #" + i.ToString();
                                }

                                fields.Fields.Insert(insertPos, newFld);

                                insertPos++;
                            }
                        }

                        repetitions = 1;

                        fld = fields.Fields[fldIdx];
                    }
                }

                for (int j = 0; j < repetitions; j++)
                {
                    if (((!fld.Skip) &&
                      (!String.IsNullOrEmpty(fld.DependentField) && KVPairs.ContainsKey(fld.DependentField)) &&
                      (fld.DependentValue.Count == 0 || fld.DependentValue.Contains(KVPairs.Item(fld.DependentField)))) ||
                      (String.IsNullOrEmpty(fld.DependentField)) ||
                      (!String.IsNullOrEmpty(fld.DependentField) && !KVPairs.ContainsKey(fld.DependentField) && fld.DependentValue.Count == 0))
                    {
                        string val = "";

                        if (fld.SkipUntilValid)
                        {
                            try
                            {
                                do
                                {
                                    val = msg.MessageData.Substring(msg.CurrentIndex, fld.Length);
                                    if (fld.ValidValues.Contains(val))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        msg.AdvanceIndex(1);
                                    }
                                }
                                while (fld.ValidValues.Contains(val));
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                if (fld.AllowNotFoundValid)
                                    val = "";
                                else
                                    throw ex;
                            }
                        }
                        else if (fld.ParseUntilValue != "")
                        {
                            string tempVal = "";
                            do
                            {
                                val = msg.MessageData.Substring(msg.CurrentIndex, 1);
                                if (fld.ParseUntilValue == val)
                                {
                                    msg.DecreaseIndex(1);
                                    break;
                                }
                                else
                                {
                                    tempVal += val;
                                    msg.AdvanceIndex(1);
                                }
                            }
                            while (true);
                            val = tempVal;
                        }
                        else
                        {
                            if (fld.DynamicLength != "")
                            {
                                foreach (MessageField scannedFld in fields.Fields)
                                {
                                    if (scannedFld.Name == fld.DynamicLength)
                                    {
                                        if (scannedFld.MessageFieldType == MessageFieldTypes.Hexadecimal)
                                        {
                                            fld.Length = Convert.ToInt32(KVPairs.Item(fld.DynamicLength), 16);
                                        }
                                        else
                                        {
                                            fld.Length = Convert.ToInt32(KVPairs.Item(fld.DynamicLength));
                                        }
                                    }
                                }
                            }
                            if (fld.Length != 0)
                            {
                                if ((fld.MessageFieldType != MessageFieldTypes.Binary))
                                {
                                    val = msg.MessageData.Substring(msg.CurrentIndex, fld.Length);
                                }
                                else
                                {
                                    val = msg.MessageData.Substring(msg.CurrentIndex, fld.Length * 2);
                                }
                            }
                            else
                            {
                                val = msg.MessageData.Substring(msg.CurrentIndex, msg.CharsLeft());
                            }
                        }
                        if(fld.OptionValues.Count == 0 || fld.OptionValues.Contains(val))
                        {
                            try
                            {
                                if (fld.ValidValues.Count > 0 || !fld.ValidValues.Contains(val))
                                {
                                    Log.Logger.MinorDebug(String.Format("Invalid value detected for field [{0}].", fld.Name));
                                    Log.Logger.MinorDebug(String.Format("Received [{0}] but can be one of [{1}]. ", val, GetCommaSeparetedListWithValues(fld.ValidValues)));
                                    throw new Exception(String.Format("Invalid value [{0}] for field [{1}].", val, fld.Name));
                                }
                                switch (fld.MessageFieldType)
                                {
                                    case MessageFieldTypes.Hexadecimal:
                                    case MessageFieldTypes.Binary:
                                        if (!Utility.IsHexString(val))
                                        {
                                            Log.Logger.MinorDebug(String.Format("Invalid value detected for field [{0}].", fld.Name));
                                            Log.Logger.MinorDebug(String.Format("Received [{0}] but expected a hexadecimal value.", val));
                                            throw new Exception(String.Format("Invalid value [{0}] for field [{1}].", val, fld.Name));
                                        }
                                        break;
                                    case MessageFieldTypes.Numeric:
                                        int Num;
                                        bool isNum = int.TryParse(fld.Repetitions, out Num);
                                        if (!isNum)
                                        {
                                            Log.Logger.MinorDebug(String.Format("Invalid value detected for field [{0}].", fld.Name));
                                            Log.Logger.MinorDebug(String.Format("Received [{0}] but expected a numeric value.", val));
                                            throw new Exception(String.Format("Invalid value [{0}] for field [{1}].", val, fld.Name));
                                        }
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (fld.RejectionCode != "")
                                {
                                    result = fld.RejectionCode;
                                }
                                else
                                    throw ex;
                            }

                            if (repetitions == 1)
                            {
                                KVPairs.Add(fld.Name, val);
                            }
                            else
                            {
                                KVPairs.Add(fld.Name + " #" + j.ToString(), val);
                            }

                            if (fld.MessageFieldType != MessageFieldTypes.Binary)
                            {
                                msg.AdvanceIndex(fld.Length);
                            }
                            else
                            {
                                msg.AdvanceIndex(fld.Length * 2);
                            }

                            if (j == repetitions)
                            {
                                fld.Skip = true;
                            }

                            if (fld.DependentField != "")
                            {
                                for (int z = fldIdx + 1; z < fields.Fields.Count - 1; z++)
                                {
                                    if (fields.Fields[z].DependentField == fld.DependentField && fields.Fields[z].ExclusiveDependency)
                                    {
                                        fields.Fields[z].Skip = true;
                                    }
                                }
                            }

                        }
                    }
                    if (msg.CharsLeft() == 0) break;

                }
                if (msg.CharsLeft() == 0) break;
                fldIdx += 1;
            }
            result = ErrorCodes.ER_00_NO_ERROR;
        }

        private static string GetCommaSeparetedListWithValues(List<string> lst)
        {
            string s = "";
            for (int i = 0; i < lst.Count - 1; i++)
            {
                if (i < lst.Count - 1)
                    s = s + lst.ElementAt(i) + ",";
                else
                    s = s + lst.ElementAt(i);
            }
            return s;
        }
    }
}
