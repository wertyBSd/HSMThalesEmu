using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.XML
{
    public class MessageField
    {
        private string m_name;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private int m_length;

        public int Length
        {
            get { return m_length; }
            set { m_length = value; }
        }

        private string m_dynamicLength;

        public string DynamicLength
        {
            get { return m_dynamicLength; }
            set { m_dynamicLength = value; }
        }

        private string m_parseUntilValue = "";

        public string ParseUntilValue
        {
            get { return m_parseUntilValue; }
            set { m_parseUntilValue = value; }
        }

        private MessageFieldTypes m_msgFieldType;

        public MessageFieldTypes MessageFieldType
        {
            get { return m_msgFieldType; }
            set { m_msgFieldType = value; }
        }

        private string m_dependentField;

        public string DependentField
        {
            get { return m_dependentField; }
            set { m_dependentField = value; }
        }

        private List<string> m_dependentValue = new List<string>();

        public List<string> DependentValue
        {
            get { return m_dependentValue; }
            set { m_dependentValue = value; }
        }

        private bool m_exclusiveDependency = true;

        public bool ExclusiveDependency
        {
            get { return m_exclusiveDependency; }
            set { m_exclusiveDependency = value; }
        }

        private List<string> m_validValues = new List<string>();

        public List<string> ValidValues
        {
            get { return m_validValues; }
            set { m_validValues = value; }
        }

        private List<string> m_optionValues = new List<string>();

        public List<string> OptionValues
        {
            get { return m_validValues; }
            set { m_validValues = value; }
        }

        private string m_rejectionCode;

        public string RejectionCode
        {
            get { return m_rejectionCode; }
            set { m_rejectionCode = value; }
        }

        private bool m_skip;

        public bool Skip
        {
            get { return m_skip; }
            set { m_skip = value; }
        }

        private string m_repetitions;

        public string Repetitions
        {
            get { return m_repetitions; }
            set { m_repetitions = value; }
        }

        private bool m_staticRepetitions = false;

        public bool StaticRepetitions
        {
            get { return m_staticRepetitions; }
            set { m_staticRepetitions = value; }
        }

        private bool m_skipUntil = false;

        public bool SkipUntilValid
        {
            get { return m_skipUntil; }
            set { m_skipUntil = value; }
        }

        private bool m_allowNotFoundValid;

        public bool AllowNotFoundValid
        {
            get { return m_allowNotFoundValid; }
            set { m_allowNotFoundValid = value; }
        }

        public void SetDependentValue(string s)
        {
            string[] sSplit = s.Split(',');
            m_dependentValue.Clear();
            foreach (string Str in sSplit)
                m_dependentValue.Add(Str);
        }

        public MessageField Clone()
        {
            MessageField o = new MessageField();
            o.DependentField = this.DependentField;
            o.DependentValue = CloneStringList(this.DependentValue);
            o.ExclusiveDependency = this.ExclusiveDependency;
            o.Length = this.Length;
            o.DynamicLength = this.DynamicLength;
            o.ParseUntilValue = this.ParseUntilValue;
            o.MessageFieldType = this.MessageFieldType;
            o.Name = this.Name;
            o.OptionValues = CloneStringList(this.OptionValues);
            o.RejectionCode = this.RejectionCode;
            o.Repetitions = this.Repetitions;
            o.Skip = this.Skip;
            o.StaticRepetitions = this.StaticRepetitions;
            o.ValidValues = CloneStringList(this.ValidValues);
            o.SkipUntilValid = this.SkipUntilValid;
            o.AllowNotFoundValid = this.AllowNotFoundValid;
            return o;
        }

        private List<string> CloneStringList(List<string> lst)
        {
            List<string> newLst = new List<string>();
            foreach (string s in lst)
                newLst.Add(s);
            return newLst;
        }
    }
}
