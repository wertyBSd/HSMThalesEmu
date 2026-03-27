using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("NK", "NL", "", "Allows multiple commands to be sent as a bundle.")]
    public class CommandChaining_NK : AHostCommand
    {
        private string _rawMessage = "";

        public CommandChaining_NK()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            // preserve raw message for fallback parsing
            _rawMessage = msg.MessageData;
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            // Execute nested sub-commands in sequence and collect their responses.
            try
            {
                int num = 0;
                if (!int.TryParse(kvp.ItemOptional("Number Of Commands"), out num)) num = 0;

                var explorer = new global::HostCommands.CommandExplorer();
                System.Text.StringBuilder sbSubResponses = new System.Text.StringBuilder();

                for (int i = 0; i < num; i++)
                {
                    string key = (i == 0) ? "SubCommand Data" : $"SubCommand Data #{i}";
                    string sub = kvp.ItemOptional(key);
                    if (String.IsNullOrEmpty(sub)) continue;

                    // sub contains the full sub-command payload (including 2-char command code)
                    string commandCode = sub.Length >= 2 ? sub.Substring(0, 2) : "";
                    string subPayload = sub.Length > 2 ? sub.Substring(2) : "";
                    var subMsg = new ThalesCore.Message.Message(subPayload);
                    var cc = explorer.GetLoadedCommand(commandCode);
                    if (cc == null)
                    {
                        // unknown subcommand: append an error code for that sub-response and continue
                        sbSubResponses.Append(ErrorCodes.ER_91_LRC_ERROR);
                        continue;
                    }

                    var hostCmd = (AHostCommand)Activator.CreateInstance(cc.DeclaringType);
                    hostCmd.AcceptMessage(subMsg);

                    MessageResponse subResp;
                    if (!String.IsNullOrEmpty(hostCmd.XMLParseResult) && hostCmd.XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
                    {
                        subResp = new MessageResponse();
                        subResp.AddElement(hostCmd.XMLParseResult);
                    }
                    else
                        subResp = hostCmd.ConstructResponse();

                    if (subResp != null)
                        sbSubResponses.Append(subResp.MessageData ?? "");

                    hostCmd.Terminate();
                }

                // If parser didn't populate SubCommand Data entries (fallback), try manual parsing
                if (sbSubResponses.Length == 0 && !String.IsNullOrEmpty(_rawMessage))
                {
                    try
                    {
                        int pos = 0;
                        // header flag
                        if (_rawMessage.Length <= pos) throw new Exception("NK: message too short");
                        pos += 1;
                        if (_rawMessage.Length < pos + 2) throw new Exception("NK: missing command count");
                        string numStr = _rawMessage.Substring(pos, 2);
                        pos += 2;
                        int count = 0;
                        int.TryParse(numStr, out count);
                        for (int k = 0; k < count; k++)
                        {
                            if (_rawMessage.Length < pos + 4) break;
                            string lenStr = _rawMessage.Substring(pos, 4);
                            pos += 4;
                            int len = 0;
                            int.TryParse(lenStr, out len);
                            if (len <= 0 || _rawMessage.Length < pos + len) break;
                            string sub = _rawMessage.Substring(pos, len);
                            pos += len;

                            string commandCode = sub.Length >= 2 ? sub.Substring(0, 2) : "";
                            string subPayload = sub.Length > 2 ? sub.Substring(2) : "";

                            var cc = explorer.GetLoadedCommand(commandCode);
                            if (cc == null)
                            {
                                sbSubResponses.Append(ErrorCodes.ER_91_LRC_ERROR);
                                continue;
                            }
                            var hostCmd = (AHostCommand)Activator.CreateInstance(cc.DeclaringType);
                            hostCmd.AcceptMessage(new ThalesCore.Message.Message(subPayload));
                            MessageResponse subResp;
                            if (!String.IsNullOrEmpty(hostCmd.XMLParseResult) && hostCmd.XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
                            {
                                subResp = new MessageResponse();
                                subResp.AddElement(hostCmd.XMLParseResult);
                            }
                            else
                                subResp = hostCmd.ConstructResponse();

                            if (subResp != null)
                                sbSubResponses.Append(subResp.MessageData ?? "");

                            hostCmd.Terminate();
                        }
                    }
                    catch { }
                }

                // Top-level status
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                // Second element: concatenated raw sub-responses
                mr.AddElement(sbSubResponses.ToString());
            }
            catch (Exception ex)
            {
                Log.Logger.MinorInfo("CommandChaining_NK: nested execution error: " + ex.Message);
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
            }

            return mr;
        }
    }
}
