using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HostCommands;
using ThalesCore.Message;
using ThalesCore.HostCommands.BuildIn;

namespace ThalesCore.Tests
{
    [TestClass]
    public class HostCommandTests
    {
        private ThalesMain o;

        [TestInitialize]
        public void InitTests()
        {
            o = new ThalesMain();
            o.MajorLogEvent += O_MajorLogEvent;
            o.MinorLogEvent += O_MinorLogEvent;
            o.StartUpWithoutTCP(@"..\..\..\ThalesCore\ThalesParameters.xml");
        }

        private void O_MinorLogEvent(ThalesMain sender, string s)
        {
            
        }

        private void O_MajorLogEvent(ThalesMain sender, string s)
        {
            
        }

        [TestCleanup]
        public void EndTests()
        {
            o.ShutDown();
            o = null;
        }

        private string TestTran(string input, AHostCommand HC)
        {
            MessageResponse retMsg;
            Message.Message msg = new Message.Message(input);

            string trailingChars = "";
            if (ExpectTrailers())
                trailingChars = msg.GetTrailers();

            HC.AcceptMessage(msg);

            if (HC.XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
            {
                retMsg = new MessageResponse();
                retMsg.AddElement(HC.XMLParseResult);
            }
            else
                retMsg = HC.ConstructResponse();

            retMsg.AddElement(trailingChars);

            HC.Terminate();
            HC = null;
            return retMsg.MessageData;
        }

        private bool ExpectTrailers()
        {
            return (bool)Resources.GetResource(Resources.EXPECT_TRAILERS);
        }

        [TestMethod]
        public void TestGenerateZPK()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
        }

        private void SwitchToDoubleLengthZMKs()
        {
            Resources.UpdateResource(Resources.DOUBLE_LENGTH_ZMKS, true);
            ClearMessageFieldStoreStore();
        }

        private void ClearMessageFieldStoreStore()
        {
            Message.XML.MessageFieldsStore.Clear();
        }

        private void AuthorizedStateOn()
        {
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, true);
        }
    }
}
