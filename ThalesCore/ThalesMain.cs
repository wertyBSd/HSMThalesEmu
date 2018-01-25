using HostCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ThalesCore.Log;
using ThalesCore.TCP;

namespace ThalesCore
{
    public class ThalesMain: ILogProcs
    {
        private int port;
        private int consolePort;
        private int maxCons;
        private int curCons = 0;
        private int consoleCurCons  = 0;
        private string LMKFile;
        private string VBsources;
        private bool CheckLMKParity;
        private string HostDefsDir;
        private bool DoubleLengthZMKs;
        private bool LegacyMode;
        private bool ExpectTrailers;
        private int HeaderLength;
        private bool EBCDIC;

        private Thread LT;
        private Thread CLT;

        private CommandExplorer CE;

        private ConsoleCommands.ConsoleCommandExplorer CCE;

        private TCP.WorkerClient[] WC;

        private TCP.WorkerClient CWC;

        private TcpListener SL;

        private TcpListener CSL;

        private ConsoleCommands.AConsoleCommand curMsg = null;

        public object VBSources { get { return VBsources; } }

        public delegate void CommandCalledMethod(ThalesMain sender, string commandCode);

        public event CommandCalledMethod CommandCalled;

        public delegate void MajorLogMethod(ThalesMain sender, string s);

        public event MajorLogMethod MajorLogEvent;

        public delegate void MinorLogMethod(ThalesMain sender, string s);

        public event MinorLogMethod MinorLogEvent;

        public delegate void PrinterDataMethod(ThalesMain sender, string s);

        public event PrinterDataMethod PrinterData;

        public delegate void DataArrivedMethod(ThalesMain sender, TCPEventArgs e);

        public event DataArrivedMethod DataArrived;

        public delegate void DataSentMethod(ThalesMain sender, TCPEventArgs e);

        public event DataSentMethod DataSent;

        public void StartUp(string XMLParameterFile)
        {
            //this.CommandCalled += ThalesMain_CommandCalled;
            StartCrypto(XMLParameterFile);
            StartTCP();
        }

        private void ThalesMain_CommandCalled(ThalesMain sender, string commandCode)
        {
            //Падает ошибка, потому что на тестах ни кто не подписывается на событие;
        }

        public void StartUpWithoutTCP(string XMLParameterFile)
        {
            StartCrypto(XMLParameterFile);
        }

        public string SayConfiguration()
        {
            return "Host command port: " + port.ToString() + System.Environment.NewLine + 
               "Console port: " + consolePort.ToString() + System.Environment.NewLine + 
               "Maximum connections: " + maxCons.ToString() + System.Environment.NewLine + 
               "Log level: " + Logger.CurrentLogLevel.ToString() + System.Environment.NewLine + 
               "Check LMK parity: " + CheckLMKParity.ToString() + System.Environment.NewLine + 
               "XML host command definitions: " + HostDefsDir + System.Environment.NewLine + 
               "Use double-length ZMKs: " + DoubleLengthZMKs.ToString() + System.Environment.NewLine + 
               "Header length: " + HeaderLength.ToString() + System.Environment.NewLine + 
               "EBCDIC: " + EBCDIC.ToString();
        }


        private void StartTCP()
        {
            StartThread(LT, ListenerThread, "TCP listening");
            StartThread(CLT, ConsoleListenerThread, "Console TCP listening");

            Logger.MajorInfo("Startup complete");
        }


        private void StartThread(Thread t, System.Threading.ThreadStart threadStart, string threadMsg)
        {
            Logger.MajorVerbose(String.Format("Starting up the {0} thread...", threadMsg));
            t = new Thread(threadStart);
            t.IsBackground = true;
            try
            {
                t.Start();
                int cntr = 0;
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Logger.MajorError(String.Format("Error while starting the {0} thread: " + ex.ToString(), threadMsg));
                throw ex;
            }
        }

        private void StartCrypto(string XMLParameterFile)
        {
            Logger.LogInterface = this;

            Resources.CleanUp();

            if (!ReadXMLFile(XMLParameterFile))
            {
                Logger.MajorError("Trying to load key/value file for Mono...");
                SetDefaultConfiguration();
            }

            Logger.MajorDebug("Searching for host command implementors...");
            CE = new CommandExplorer();
            Logger.MinorInfo("Loaded commands dump" + System.Environment.NewLine + CE.GetLoadedCommands());

            Logger.MajorDebug("Searching for console command implementors...");
            CCE = new ConsoleCommands.ConsoleCommandExplorer();
            Logger.MinorInfo("Loaded console commands dump " + System.Environment.NewLine + CCE.GetLoadedCommands());
        }

        private bool ReadXMLFile(string fileName)
        {
            try
            {
                Logger.MajorDebug("Reading XML configuration...");
                System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(fileName);
                reader.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
                reader.MoveToContent();
                reader.Read();

                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

                doc.Load(reader);

                port = Convert.ToInt32(GetParameterValue(doc, "Port"));
                consolePort = Convert.ToInt32(GetParameterValue(doc, "ConsolePort"));
                maxCons = Convert.ToInt32(GetParameterValue(doc, "MaxConnections"));
                LMKFile = Convert.ToString(GetParameterValue(doc, "LMKStorageFile"));
                VBsources = Convert.ToString(GetParameterValue(doc, "VBSourceDirectory"));
                Logger.CurrentLogLevel = (Log.Logger.LogLevel)(Enum.Parse(typeof(Logger.LogLevel), Convert.ToString(GetParameterValue(doc, "LogLevel")), true));
                CheckLMKParity = Convert.ToBoolean(GetParameterValue(doc, "CheckLMKParity"));
                HostDefsDir = Convert.ToString(GetParameterValue(doc, "XMLHostDefinitionsDirectory"));
                DoubleLengthZMKs = Convert.ToBoolean(GetParameterValue(doc, "DoubleLengthZMKs"));
                LegacyMode = Convert.ToBoolean(GetParameterValue(doc, "LegacyMode"));
                ExpectTrailers = Convert.ToBoolean(GetParameterValue(doc, "ExpectTrailers"));
                HeaderLength = Convert.ToInt32(GetParameterValue(doc, "HeaderLength"));
                EBCDIC = Convert.ToBoolean(GetParameterValue(doc, "EBCDIC"));

                StartUpCore(Convert.ToString(GetParameterValue(doc, "FirmwareNumber")), Convert.ToString(GetParameterValue(doc, "DSPFirmwareNumber")),
                    Convert.ToBoolean(GetParameterValue(doc, "StartInAuthorizedState")), Convert.ToInt32(GetParameterValue(doc, "ClearPINLength")));

                reader.Close();
                reader = null;

                return true;
            }
            catch (Exception ex)
            {
                Logger.MajorError("Error loading the configuration file");
                Logger.MajorError(ex.ToString());
                return true;
            }
        }

        private bool TryToReadValuePairFile(string fileName)
        {
            try
            {
                SortedList<string, string> list = new SortedList<string, string>();

                using (StreamReader SR = new StreamReader(fileName, System.Text.Encoding.Default))
                {
                    while (SR.Peek() > -1)
                    {
                        string s = SR.ReadLine();
                        if (!(String.IsNullOrEmpty(s)) || (s.StartsWith(";")))
                        {
                            string[] sSplit = s.Split('=');
                            list.Add(sSplit[0], sSplit[1]);
                        }
                    }
                }
                port = Convert.ToInt32(list["PORT"]);
                consolePort = Convert.ToInt32(list["CONSOLEPORT"]);
                maxCons = Convert.ToInt32(list["MAXCONNECTIONS"]);
                LMKFile = list["LMKSTORAGEFILE"];
                VBsources = list["VBSOURCEDIRECTORY"];
                Logger.CurrentLogLevel = (Log.Logger.LogLevel)(Enum.Parse(typeof(Logger.LogLevel), Convert.ToString(list["LOGLEVEL"]), true));
                CheckLMKParity = Convert.ToBoolean(list["CHECKLMKPARITY"]);
                HostDefsDir = list["XMLHOSTDEFINITIONSDIRECTORY"];
                DoubleLengthZMKs = Convert.ToBoolean(list["DOUBLELENGTHZMKS"]);
                LegacyMode = Convert.ToBoolean(list["LEGACYMODE"]);
                ExpectTrailers = Convert.ToBoolean(list["EXPECTTRAILERS"]);
                HeaderLength = Convert.ToInt32(list["HEADERLENGTH"]);
                EBCDIC = Convert.ToBoolean(list["EBCDIC"]);

                if (HostDefsDir == "")
                    HostDefsDir = Utility.GetExecutingDirectory();
                if (VBsources == "")
                    VBsources = Utility.GetExecutingDirectory();

                StartUpCore(list["FIRMWARENUMBER"], list["DSPFIRMWARENUMBER"], Convert.ToBoolean(list["STARTINAUTHORIZEDSTATE"]), Convert.ToInt32(list["CLEARPINLENGTH"]));

                return true;
            }
            catch (Exception ex)
            {
                Logger.MajorError("Error loading key/value file");
                Logger.MajorError(ex.ToString());
                return false;
            }

        }

        private void SetDefaultConfiguration()
        {
            Logger.MajorDebug("Using default configuration...");
            port = 9998;
            consolePort = 9997;
            maxCons = 5;
            LMKFile = "";
            VBsources = Utility.GetExecutingDirectory();
            Logger.CurrentLogLevel = Logger.LogLevel.Debug;
            CheckLMKParity = true;
            HostDefsDir = Utility.GetExecutingDirectory();
            DoubleLengthZMKs = false;
            LegacyMode = false;
            ExpectTrailers = false;
            HeaderLength = 4;
            EBCDIC = false;

            StartUpCore("0007-E000", "0001", true, 4);
        }

        private void StartUpCore(string firmwareNumber, string dspFirmwareNumber, bool startInAuthorizedState, int clearPINLength)
        {
            CompileAndLoad(VBsources);

            Resources.AddResource(Resources.CONSOLE_PORT, consolePort);
            Resources.AddResource(Resources.WELL_KNOWN_PORT, port);
            Resources.AddResource(Resources.FIRMWARE_NUMBER, firmwareNumber);
            Resources.AddResource(Resources.DSP_FIRMWARE_NUMBER, dspFirmwareNumber);
            Resources.AddResource(Resources.MAX_CONS, maxCons);
            Resources.AddResource(Resources.AUTHORIZED_STATE, startInAuthorizedState);
            Resources.AddResource(Resources.CLEAR_PIN_LENGTH, clearPINLength);
            Resources.AddResource(Resources.DOUBLE_LENGTH_ZMKS, DoubleLengthZMKs);
            Resources.AddResource(Resources.LEGACY_MODE, LegacyMode);
            Resources.AddResource(Resources.EXPECT_TRAILERS, ExpectTrailers);
            Resources.AddResource(Resources.HEADER_LENGTH, HeaderLength);
            Resources.AddResource(Resources.EBCDIC, EBCDIC);

            HostDefsDir = Utility.AppendDirectorySeparator(HostDefsDir);

            Resources.AddResource(Resources.HOST_COMMANDS_XML_DEFS, HostDefsDir);

            if (LMKFile == "")
            {
                Logger.MajorInfo("No LMK storage file specified, creating new keys");
                Cryptography.LMK.LMKStorage.LMKStorageFile = "LMKSTORAGE.TXT";
                Cryptography.LMK.LMKStorage.GenerateTestLMKs();
            }
            else
            {
                Logger.MajorDebug("Reading LMK storage");
                Cryptography.LMK.LMKStorage.ReadLMKs(LMKFile);
            }

            Resources.AddResource(Resources.LMK_CHECK_VALUE, Cryptography.LMK.LMKStorage.GenerateLMKCheckValue());
        }

        private void CompileAndLoad(object vBSources)
        {
            throw new NotImplementedException();
        }

        private void CompileAndLoad(string vbDir)
        {
            if (vbDir == "") return;

            string[] files = Directory.GetFiles(vbDir, "*.vb");

            for (int i = 0; i < files.GetUpperBound(0) + 1; i++)
            {
                CompileCode(files[i], "VB");
            }

            files = Directory.GetFiles(vbDir, "*.cs");

            for (int i = 0; i < files.GetUpperBound(0) + 1; i++)
            {
                CompileCode(files[i], "CSharp");
            }
        }

        private System.Reflection.Assembly CompileCode(string sourceFile, string language)
        {
            string vbSource = "";

            string fName = new FileInfo(sourceFile).Name;

            Logger.MajorVerbose("Compiling " + fName + "...");

            try
            {
                using (StreamReader SR = new StreamReader(sourceFile))
                {
                    while (SR.Peek() > -1)
                    {
                        vbSource += SR.ReadLine() + System.Environment.NewLine;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.MajorError("Exception raised while reading " + fName + System.Environment.NewLine + ex.ToString());
                return null;
            }

            System.CodeDom.Compiler.CodeDomProvider prov = null;

            System.CodeDom.Compiler.CompilerParameters compParams = new System.CodeDom.Compiler.CompilerParameters();

            System.CodeDom.Compiler.CompilerResults compResults;

            string[] refs = { "System.dll", "Microsoft.VisualBasic.dll", "System.XML.dll", "System.Data.dll", Path.GetDirectoryName(Assembly.GetAssembly(typeof(ThalesMain)).CodeBase) + "\\ThalesCore.dll", System.Reflection.Assembly.GetAssembly(typeof(ThalesMain)).Location };

            compParams.ReferencedAssemblies.AddRange(refs);

            try
            {
                prov = System.CodeDom.Compiler.CodeDomProvider.CreateProvider(language);
                compResults = prov.CompileAssemblyFromSource(compParams, vbSource);
            }

            catch (Exception ex)
            {
                Logger.MajorError("Exception raised during compilation of " + fName + System.Environment.NewLine + ex.ToString());
                return null;
            }

            if (compResults.Errors.Count > 0)
            {
                Logger.MajorError("Compilation errors of " + fName);
                foreach (System.CodeDom.Compiler.CompilerError Err in compResults.Errors)
                {
                    Logger.MajorError("Line: " + Err.Line.ToString() + System.Environment.NewLine + "Column: " + Err.Column.ToString() + System.Environment.NewLine + "Error: " + Err.ErrorText);
                }
                return null;
            }
            else
                return System.Reflection.Assembly.LoadFrom(compResults.PathToAssembly);
        }

        private string GetParameterValue(XmlDocument doc, string element)
        {
            return doc.DocumentElement[element].Attributes["value"].Value;
        }

        public void ShutDown()
        {
            if (LT == null)
            {
                try
                {
                    SL.Stop();
                    SL = null;
                }
                catch (Exception ex)
                {

                }

                Logger.MajorVerbose("Stopping the listening thread...");

                try
                {
                    LT.Abort();
                    LT = null;
                }
                catch (Exception ex)
                {

                }

                Logger.MajorVerbose("Disconnecting connected clients...");
                if(WC != null)
                for (int i = 0; i < WC.GetUpperBound(0) + 1; i++)
                {
                    try
                    {
                        if (!(WC[i] == null) && (WC[i].IsConnected == true))
                            WC[i].TermClient();
                        WC[i] = null;
                    }
                    catch (Exception ex)
                    { }
                }

                try
                {
                    CSL.Stop();
                    CSL = null;
                }
                catch (Exception ex)
                { }
                Logger.MajorVerbose("Stopping the console listening thread...");

                try
                {
                    CLT.Abort();
                    CLT = null;
                }
                catch (Exception ex)
                { }


                try
                {
                    if (!(CWC == null) && (CWC.IsConnected == true))
                        CWC.TermClient();
                    CWC = null;
                }
                catch (Exception ex)
                { }
            }
            Logger.MajorInfo("Shutdown complete");
        }

        private void ConsoleListenerThread()
        {
            try
            {
                CSL = new TcpListener(new System.Net.IPEndPoint(0, consolePort));
                CSL.Start();

                while (true)
                {
                    CWC = new TCP.WorkerClient(CSL.AcceptTcpClient());
                    CWC.InitOps();

                    CWC.Disconnected += CWCDisconnected;
                    CWC.MessageArrived += CWCMessageArrived;

                    Logger.MajorInfo("Console client from " + CWC.ClientIP() + " is connected");

                    consoleCurCons = 1;
                    while (consoleCurCons == 1)
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.MajorInfo("Exception on console listening thread (" + ex.Message + ")");
                if (CSL != null)
                {
                    CSL.Stop();
                    CSL = null;
                }
            }
        }

        

        private void ListenerThread()
        {
            WC = null;

            try
            {
                SL = new TcpListener(new System.Net.IPEndPoint(0, port));
                SL.Start();

                while (true)
                {
                    TCP.WorkerClient wClient = new TCP.WorkerClient(SL.AcceptTcpClient());
                    wClient.InitOps();

                    wClient.Disconnected += WCDisconnected;
                    wClient.MessageArrived += WCMessageArrived;

                    Logger.MajorInfo("Client from " + wClient.ClientIP() + " is connected");

                    curCons += 1;

                    bool slotedIt = false;

                    for (int i = 0; i < WC.GetUpperBound(0); i++)
                    {
                        if ((WC[i] == null) || (WC[i].IsConnected == false))
                        {
                            WC[i] = wClient;
                            slotedIt = true;
                        }
                    }

                    if (slotedIt == false)
                    {
                        Array.Resize(ref WC, WC.GetUpperBound(0));
                        WC[WC.GetUpperBound(0) - 1] = wClient;
                    }

                    while (curCons >= maxCons)
                        Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Logger.MajorInfo("Exception on listening thread (" + ex.Message + ")");
                if (SL != null)
                {
                    SL.Stop();
                    SL = null;
                }
            }
        }

        private void WCDisconnected(WorkerClient sender)
        {
            throw new NotImplementedException();
        }

        private void CWCDisconnected(TCP.WorkerClient sender)
        {
            Logger.MajorInfo("Console client disconnected.");
            sender.TermClient();

            consoleCurCons -= 1;

            curMsg = null;
        }

        private void CWCMessageArrived(WorkerClient sender, byte[] b, int len)
        {
            Message.Message msg = new Message.Message(b);

            try
            {
                if (curMsg == null)
                {
                    Logger.MajorVerbose("Client: " + sender.ClientIP() + System.Environment.NewLine + "Request: " + msg.MessageData);
                    Logger.MajorDebug("Searching for implementor of " + msg.MessageData + "...");

                    ConsoleCommands.ConsoleCommandClass CC = CCE.GetLoadedCommand(msg.MessageData);

                    if (CC == null)
                    {
                        Logger.MajorError("No implementor for " + msg.MessageData + ".");
                        sender.send("Command not found" + System.Environment.NewLine);
                    }
                    curMsg = (ConsoleCommands.AConsoleCommand)Activator.CreateInstance(CC.CommandType);
                    curMsg.InitializeStack();

                }
                else
                {
                    string returnMsg = null;

                    try
                    {
                        returnMsg = curMsg.AcceptMessage(msg.MessageData);
                    }
                    catch (Exception ex)
                    {
                        returnMsg = ex.Message;
                    }

                    if ((returnMsg != null) && (curMsg.CommandFinished))
                    {
                        sender.send(returnMsg + System.Environment.NewLine);
                        curMsg = null;
                    }
                    else
                        sender.send(curMsg.GetClientMessage());
                    return;
                }

                if (curMsg.IsNoinputCommand())
                {
                    try
                    {
                        sender.send(curMsg.ProcessMessage() + System.Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        sender.send(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.MajorError("Exception while parsing message or creating implementor instance" + System.Environment.NewLine + ex.ToString());
                Logger.MajorError("Disconnecting client.");
                sender.TermClient();
                curMsg = null;
            }
        }


        private void WCMessageArrived(TCP.WorkerClient sender, byte[] b, int len)
        {
            TCPEventArgs e = new TCPEventArgs();
            e.RemoteClient = sender.ClientIP();

            e.Data = new byte[len];

            DataArrived(this, e);

            Array.Copy(b, 0, e.Data, 0, len);

            Message.Message msg = new Message.Message(b);

            Logger.MajorVerbose("Client: " + sender.ClientIP() + System.Environment.NewLine + "Request: " + msg.MessageData);
            try
            {
                Logger.MajorDebug("Parsing header and code of message " + msg.MessageData + "...");
                string messageHeader = msg.GetSubstring(HeaderLength);

                msg.AdvanceIndex(HeaderLength);

                string commandCode = msg.GetSubstring(2);

                msg.AdvanceIndex(2);

                CommandCalled(this, commandCode);

                CommandClass CC = CE.GetLoadedCommand(commandCode);

                if (CC == null)
                {
                    Logger.MajorError("No implementor for " + commandCode + "." + System.Environment.NewLine + "Disconnecting client.");
                    sender.TermClient();
                }
                else
                {
                    Logger.MajorDebug("Found implementor " + CC.DeclaringType.FullName + ", instantiating...");
                    AHostCommand o = (AHostCommand)Activator.CreateInstance(CC.DeclaringType);
                    o = (AHostCommand)Activator.CreateInstance(CC.DeclaringType);

                    Message.MessageResponse retMsg;
                    Message.MessageResponse retMsgAfterIO;

                    try
                    {
                        string trailingChars = "";
                        if (ExpectTrailers)
                        {
                            trailingChars = msg.GetTrailers();
                        }

                        if ((CheckLMKParity == false)||(Cryptography.LMK.LMKStorage.CheckLMKStorage()))
                        {
                            //Logger.MinorInfo("=== [" + commandCode + "], starts " + Utility.getTimeMMHHSSmmmm + " =======");

                            Logger.MajorDebug("Calling AcceptMessage()...");
                            o.AcceptMessage(msg);

                            Logger.MinorVerbose(o.DumpFields());
                        }
                    }
                    catch (Exception ex)
                    { }
                }
            }
            catch (Exception ex)
            {

            }

        }


        private void RaiseDataSentEvent(string remoteClient, Message.MessageResponse msg)
        {
            TCPEventArgs e = new TCPEventArgs();
            e.RemoteClient = remoteClient;
            e.Data = Utility.GetBytesFromString(msg.MessageData);
            DataSent(this, e);
        }

        public void GetMajor(string s) 
        {
            MajorLogEvent(this, s);
        }



        public void GetMinor(string s)
        {
            MinorLogEvent(this, s);
        }
    }
}
