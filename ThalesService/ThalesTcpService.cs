using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HostCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThalesCore;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesService
{
    public class ThalesTcpService : BackgroundService
    {
        private readonly ILogger<ThalesTcpService> _logger;
        private TcpListener _listener;
        private readonly int _port;

        public ThalesTcpService(ILogger<ThalesTcpService> logger)
        {
            _logger = logger;
            var env = Environment.GetEnvironmentVariable("THALES_SERVICE_PORT");
            if (!int.TryParse(env, out _port))
                _port = 1500;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ThalesTcpService starting, listening on port {port}", _port);

            // initialize Thales environment
            var thales = new ThalesCore.ThalesMain();
            string[] candidates = new[] {
                Path.Combine("..", "ThalesCore", "ThalesParameters.xml"),
                Path.Combine("..", "..", "ThalesCore", "ThalesParameters.xml"),
                Path.Combine("..", "..", "..", "ThalesCore", "ThalesParameters.xml") };
            string paramFile = candidates.FirstOrDefault(File.Exists) ?? Path.Combine("ThalesCore", "ThalesParameters.xml");
            try { thales.StartUpWithoutTCP(paramFile); }
            catch { _logger.LogWarning("Could not load Thales parameters from {file}", paramFile); }

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            var explorer = new HostCommands.CommandExplorer();

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, explorer, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Listener exception");
                    client?.Close();
                }
            }

            _listener.Stop();
        }

        private async Task HandleClientAsync(TcpClient client, HostCommands.CommandExplorer explorer, CancellationToken ct)
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            try
            {
                int read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) return;
                var reqBytes = new byte[read];
                Array.Copy(buffer, reqBytes, read);

                var msg = new ThalesCore.Message.Message(reqBytes);
                _logger.LogInformation("Received: {req}", msg.MessageData);

                int headerLen = 4;
                try { headerLen = (int)Resources.GetResource(Resources.HEADER_LENGTH); } catch { headerLen = 4; }

                string commandCode;
                try
                {
                    string messageHeader = msg.GetSubstring(headerLen);
                    msg.AdvanceIndex(headerLen);
                    commandCode = msg.GetSubstring(2);
                    msg.AdvanceIndex(2);
                }
                catch
                {
                    commandCode = msg.MessageData.Length >= 2 ? msg.MessageData.Substring(0, 2) : "";
                }

                var cc = explorer.GetLoadedCommand(commandCode);
                if (cc == null)
                {
                    var resp = "91"; // unknown
                    var respBytes = Encoding.ASCII.GetBytes(resp);
                    await stream.WriteAsync(respBytes, ct);
                    return;
                }

                var hostCmd = (AHostCommand)Activator.CreateInstance(cc.DeclaringType);
                hostCmd.AcceptMessage(msg);
                var response = hostCmd.ConstructResponse();
                var outBytes = Encoding.ASCII.GetBytes(response.MessageData);
                await stream.WriteAsync(outBytes, ct);
                hostCmd.Terminate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client handling error");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
