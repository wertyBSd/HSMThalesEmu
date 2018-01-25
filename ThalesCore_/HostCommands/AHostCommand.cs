using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore;

namespace HostCommands
{
    public class AHostCommand
    {
        protected string m_PrinterData = "";

        protected Message.XML.MessageFields m_msgFields= new Message.XML.MessageFields();

        protected string m_XMLParseResult = ErrorCodes.ER_00_NO_ERROR;

        protected Message.XML.MessageKeyValuePairs kvp = new Message.XML.MessageKeyValuePairs();

        public Message.XML.MessageFields XMLMessageFields
        {
            get { return m_msgFields; }
            set {  m_msgFields = value; }
        }

        public string XMLParseResult
        {
            get { return m_XMLParseResult; }
            set { m_XMLParseResult = value; }
        }

        public Message.XML.MessageKeyValuePairs KeyValuePairs
        {
            get { return kvp; }
        }

        public string PrinterData
        {
            get { return m_PrinterData; }
        }

        //override public void AcceptMessage(Message.XML.)
        //{
        //}
    }
}
