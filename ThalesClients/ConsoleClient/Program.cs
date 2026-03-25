using System.Net.Sockets;
using System.Text;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Thales Console Client");
        string host = "127.0.0.1";
        int port = 1500;
        if (args.Length >= 1) host = args[0];
        if (args.Length >= 2 && int.TryParse(args[1], out var p)) port = p;
        Console.WriteLine($"Connecting to {host}:{port}");

        while (true)
        {
            Console.Write("Enter command (or 'quit'): ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port);
                var stream = tcp.GetStream();
                var data = Encoding.ASCII.GetBytes(line);
                await stream.WriteAsync(data, 0, data.Length);
                var buffer = new byte[4096];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                var resp = Encoding.ASCII.GetString(buffer, 0, read);
                Console.WriteLine($"Response: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return 0;
    }
}
