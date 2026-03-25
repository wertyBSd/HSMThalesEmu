using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class MainForm : Form
{
    TextBox txtHost = new TextBox { Text = "127.0.0.1", Width = 120 };
    NumericUpDown numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 1500 };
    ComboBox cmbAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    Button btnSend = new Button { Text = "Send", Width = 80 };
    TextBox txtResponse = new TextBox { Multiline = true, ReadOnly = true, Height = 200, Width = 380, ScrollBars = ScrollBars.Vertical };
    CheckBox chkFlag = new CheckBox { Text = "Include extra flag" };

    Dictionary<string, string> actions = new Dictionary<string, string>
    {
        { "Echo sample", "00" },
        { "Echo with data", "B2" },
        { "Unknown command (test)", "ZZ" }
    };

    public MainForm()
    {
        Text = "Thales UI Client";
        ClientSize = new Size(420, 360);

        Controls.Add(new Label { Text = "Host:", Location = new Point(10, 14) });
        txtHost.Location = new Point(60, 10);
        Controls.Add(txtHost);

        Controls.Add(new Label { Text = "Port:", Location = new Point(200, 14) });
        numPort.Location = new Point(240, 10);
        Controls.Add(numPort);

        Controls.Add(new Label { Text = "Action:", Location = new Point(10, 50) });
        cmbAction.Location = new Point(60, 46);
        foreach (var k in actions.Keys) cmbAction.Items.Add(k);
        cmbAction.SelectedIndex = 0;
        Controls.Add(cmbAction);

        chkFlag.Location = new Point(60, 80);
        Controls.Add(chkFlag);

        btnSend.Location = new Point(60, 110);
        btnSend.Click += async (s, e) => await SendActionAsync();
        Controls.Add(btnSend);

        txtResponse.Location = new Point(10, 150);
        Controls.Add(txtResponse);
    }

    async Task SendActionAsync()
    {
        btnSend.Enabled = false;
        txtResponse.Clear();
        string host = txtHost.Text.Trim();
        int port = (int)numPort.Value;
        string actionKey = (string)cmbAction.SelectedItem;
        if (actionKey == null) { btnSend.Enabled = true; return; }
        string code = actions[actionKey];
        string payload = code;
        if (chkFlag.Checked) payload += "F"; 

        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port);
            var stream = tcp.GetStream();
            var data = Encoding.ASCII.GetBytes(payload);
            await stream.WriteAsync(data, 0, data.Length);
            var buffer = new byte[4096];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            var resp = Encoding.ASCII.GetString(buffer, 0, read);
            txtResponse.Text = resp;
        }
        catch (Exception ex)
        {
            txtResponse.Text = "Error: " + ex.Message;
        }
        finally
        {
            btnSend.Enabled = true;
        }
    }
}
