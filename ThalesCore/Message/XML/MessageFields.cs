using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore;

namespace Message.XML
{
    public class MessageFields
    {
        private List<MessageField> m_fields;

        public List<MessageField> Fields
        {
            get { return m_fields; }
        }

        private bool m_isDynamic = false;

        public bool IsDynamic
        {
            get { return m_isDynamic; }
            set { m_isDynamic = value; }
        }

        public static MessageFields ReadXMLFields(string xmlFile)
        {
            return RecursiveReadXMLFields(Convert.ToString(ThalesCore.Resources.GetResource(ThalesCore.Resources.HOST_COMMANDS_XML_DEFS)) + xmlFile);
        }

        private static MessageFields RecursiveReadXMLFields(string xmlFile)
        {
            MessageFields fields = new MessageFields();
            fields.m_fields = new List<MessageField>();
            using (System.Data.DataSet ds = new System.Data.DataSet())
            {
                ds.ReadXml(xmlFile);
                foreach (System.Data.DataRow dr in ds.Tables["Field"].Rows)
                {
                    MessageField fld = new MessageField();

                    fld.Name = Convert.ToString(dr["Name"]);

                    if (ContainsNonNullColumn(dr, "DynamicFieldLength")) fld.DynamicLength = Convert.ToString(dr["DynamicFieldLength"]);

                    if (ContainsNonNullColumn(dr, "ParseUntilValue")) fld.ParseUntilValue = Convert.ToString(dr["ParseUntilValue"]);

                    if (ContainsNonNullColumn(dr, "DependentField")) fld.DependentField = Convert.ToString(dr["DependentField"]);
                    if (ContainsNonNullColumn(dr, "DependentValue")) fld.SetDependentValue(Convert.ToString(dr["DependentValue"]));


                    if (ContainsNonNullColumn(dr, "ExclusiveDependency")) fld.ExclusiveDependency = Convert.ToBoolean(dr["ExclusiveDependency"]);

                    if (ContainsNonNullColumn(dr, "RejectionCodeIfInvalid")) fld.RejectionCode = Convert.ToString(dr["DynamicFieldLength"]);

                    if (ContainsNonNullColumn(dr, "Repetitions")) fld.Repetitions = Convert.ToString(dr["Repetitions"]);


                    if (ContainsNonNullColumn(dr, "StaticRepetitions")) fld.StaticRepetitions = Convert.ToBoolean(dr["StaticRepetitions"]);


                    if (ContainsNonNullColumn(dr, "SkipUntilValid")) fld.SkipUntilValid = Convert.ToBoolean(dr["SkipUntilValid"]);

                    if (ContainsNonNullColumn(dr, "AllowNotFoundValidValue")) fld.AllowNotFoundValid = Convert.ToBoolean(dr["AllowNotFoundValidValue"]);

                    if (ContainsNonNullColumn(dr, "OptionValue")) fld.OptionValues.Add(Convert.ToString(dr["DynamicFieldLength"]));

                    if (ContainsNonNullColumn(dr, "ValidValue")) fld.ValidValues.Add(Convert.ToString(dr["ValidValue"]));

                    if (ContainsNonNullColumn(dr, "field_id"))
                    {
                        int id = Convert.ToInt32(dr["field_id"]);

                        if (ds.Tables["OptionValue"] != null)
                        {
                            foreach (System.Data.DataRow drOption in ds.Tables["OptionValue"].Select("field_id=" + id.ToString()))
                            {
                                try
                                {
                                    fld.OptionValues.Add(Convert.ToString(drOption["OptionValue_Text"]));
                                }
                                catch (Exception ex)
                                {
                                    fld.OptionValues.Add(Convert.ToString(drOption["OptionValue_Column"]));
                                }
                            }
                        }

                        if (ds.Tables["ValidValue"] != null)
                        {
                            foreach (System.Data.DataRow drOption in ds.Tables["ValidValue"].Select("field_id=" + id.ToString()))
                            {
                                try
                                {
                                    fld.OptionValues.Add(Convert.ToString(drOption["ValidValue_Text"]));
                                }
                                catch (Exception ex)
                                {
                                    fld.OptionValues.Add(Convert.ToString(drOption["ValidValue_Column"]));
                                }
                            }
                        }
                    }

                    if (ContainsNonNullColumn(dr, "IncludeFile"))
                    {
                        System.IO.FileInfo FI = new System.IO.FileInfo(xmlFile);
                        MessageFields includeFields = new MessageFields();
                        includeFields = RecursiveReadXMLFields(Utility.AppendDirectorySeparator(FI.Directory.FullName) + Convert.ToString(dr["IncludeFile"]));

                        foreach (MessageField inclFld in includeFields.Fields)
                        {
                            inclFld.Name = inclFld.Name.Replace("#replace#", fld.Name);


                            if (!String.IsNullOrEmpty(inclFld.DependentField))
                                inclFld.DependentField = inclFld.DependentField.Replace("#replace#", fld.Name);

                            if (!String.IsNullOrEmpty(inclFld.DynamicLength))
                                inclFld.DynamicLength = inclFld.DynamicLength.Replace("#replace#", fld.Name);

                            inclFld.OptionValues.AddRange(fld.OptionValues);
                            inclFld.ValidValues.AddRange(fld.ValidValues);

                            if ((String.IsNullOrEmpty(fld.DependentField) == false) || String.IsNullOrEmpty(inclFld.DependentField))
                            {
                                inclFld.DependentField = fld.DependentField;
                                inclFld.DependentValue = fld.DependentValue;
                                inclFld.ExclusiveDependency = fld.ExclusiveDependency;
                            }

                            if ((String.IsNullOrEmpty(fld.Repetitions) == false) || (String.IsNullOrEmpty(inclFld.Repetitions)))
                            {
                                inclFld.Repetitions = fld.Repetitions;
                                inclFld.StaticRepetitions = fld.StaticRepetitions;
                            }

                            fields.Fields.Add(inclFld);
                        }
                    }
                    else
                    {
                        string len = Convert.ToString(dr["Length"]);
                        if (Char.IsNumber(len, 0))
                        {
                            if (fld.ParseUntilValue == "")
                                fld.Length = Convert.ToInt32(len);
                            else
                                fld.Length = 1;

                        }
                        else
                        {
                            switch (len)
                            {
                                case Resources.DOUBLE_LENGTH_ZMKS:
                                    fields.IsDynamic = true;
                                    if (Convert.ToBoolean(Resources.GetResource(Resources.DOUBLE_LENGTH_ZMKS)) == true)
                                        fld.Length = 32;
                                    else
                                        fld.Length = 16;
                                    break;
                                case Resources.CLEAR_PIN_LENGTH:
                                    fields.IsDynamic = true;
                                    fld.Length = Convert.ToInt32(Resources.GetResource(Resources.CLEAR_PIN_LENGTH)) + 1;
                                    break;
                                default:
                                    throw new ThalesCore.Exceptions.XInvalidConfiguration(String.Format("Invalid length element [{0}]", len));
                            }
                        }
                    }
                    try
                    {
                        fld.MessageFieldType = (MessageFieldTypes)Enum.Parse(typeof(MessageFieldTypes), dr["Type"].ToString(), true);
                        fields.Fields.Add(fld);
                    }
                    catch (Exception ex)
                    { }
                }
            }

            return fields;
        }

        private static bool ContainsNonNullColumn(DataRow dr, string columnName)
        {
            try
            {
                return !Convert.IsDBNull(dr[columnName]);
            }
            catch (ArgumentException ex)
            {
                return false;
            }
        }

        public MessageFields Clone()
        {
            MessageFields o = new MessageFields();
            foreach (MessageField field in m_fields)
                o.Fields.Add(field.Clone());
            o.IsDynamic = this.IsDynamic;
            return o;
        }
    }
}
