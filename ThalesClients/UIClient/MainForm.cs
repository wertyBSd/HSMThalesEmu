using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class MainForm : Form
{
    private readonly TextBox txtHost = new TextBox { Text = "127.0.0.1" };
    private readonly NumericUpDown numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 1500 };
    private readonly ComboBox cmbAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button btnSend = new Button { Text = "Send" };
    private readonly TextBox txtResponse = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly CheckBox chkFlag = new CheckBox { Text = "Include extra flag" };

    // Simplified action set: display-friendly names mapped to internal short codes
    // Map UI action names to real Thales request codes (from XML defs)
    private readonly Dictionary<string, string> actions = new Dictionary<string, string>
    {
        { "Echo", "B2" },                 // EchoTest_B2
        { "Import Master Key", "A6" },    // ImportKey_A6
        { "Export Master Key (masked)", "A8" }, // ExportKey_A8
        { "Translate PIN (TPK->LMK)", "JC" }, // TranslatePINFromTPKToLMK_JC
        { "Verify PIN (IBM)", "DA" }      // VerifyTerminalPINwithIBMAlgorithm_DA
    };

    // Dynamic inputs for selected action
    private readonly Panel actionDetails = new Panel { AutoSize = true, Dock = DockStyle.Fill };
    private readonly TextBox txtParam1 = new TextBox();
    private readonly TextBox txtParam2 = new TextBox();

    public MainForm()
    {
        Text = "Thales UI Client";
        MinimumSize = new Size(460, 420);

        // Main layout
        var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 5 };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // connection row
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // action row
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // action details row
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // button row
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // response

        // Connection row (host + port)
        var connPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        connPanel.Controls.Add(new Label { Text = "Host:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        txtHost.Width = 140;
        connPanel.Controls.Add(txtHost);
        connPanel.Controls.Add(new Label { Text = "Port:", AutoSize = true, Margin = new Padding(12, 6, 3, 3) });
        numPort.Width = 90;
        connPanel.Controls.Add(numPort);

        // Action row
        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        actionPanel.Controls.Add(new Label { Text = "Action:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        cmbAction.Width = 260;
        foreach (var k in actions.Keys) cmbAction.Items.Add(k);
        if (cmbAction.Items.Count > 0) cmbAction.SelectedIndex = 0;
        UpdateActionDetails();
        actionPanel.Controls.Add(cmbAction);
        actionPanel.Controls.Add(chkFlag);
        cmbAction.SelectedIndexChanged += (s, e) => UpdateActionDetails();

        // Action details row (dynamic inputs)
        actionDetails.Padding = new Padding(0, 6, 0, 6);

        // Button row
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        btnSend.Width = 100;
        btnSend.Height = 30;
        btnSend.Click += async (s, e) => await SendActionAsync();
        btnPanel.Controls.Add(btnSend);

        // Response (fills remaining space)
        var respPanel = new Panel { Dock = DockStyle.Fill };
        txtResponse.Location = new Point(0, 0);
        txtResponse.Margin = new Padding(0);
        respPanel.Controls.Add(txtResponse);

        main.Controls.Add(connPanel, 0, 0);
        main.Controls.Add(actionPanel, 0, 1);
        main.Controls.Add(actionDetails, 0, 2);
        main.Controls.Add(btnPanel, 0, 3);
        main.Controls.Add(respPanel, 0, 4);

        Controls.Add(main);
    }

    private void UpdateActionDetails()
    {
        actionDetails.Controls.Clear();
        string actionKey = cmbAction.SelectedItem as string ?? string.Empty;
        var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };

        void AddField(string label, TextBox tb, int width = 240)
        {
            var lbl = new Label { Text = label, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 6, 0) };
            tb.Width = width;
            fl.Controls.Add(lbl);
            fl.Controls.Add(tb);
        }

        switch (actionKey)
        {
            case "Echo":
                txtParam1.Text = "Hello";
                AddField("Message:", txtParam1);
                break;
            case "Import Master Key":
                txtParam1.Text = "LMK01"; // label
                txtParam2.Text = "0123456789ABCDEF"; // key hex
                AddField("Key label:", txtParam1);
                AddField("Key (hex):", txtParam2);
                break;
            case "Export Master Key (masked)":
                txtParam1.Text = "LMK01";
                AddField("Key label:", txtParam1);
                break;
            case "Translate PIN":
                txtParam1.Text = "1234"; // PIN
                txtParam2.Text = "4000000000000002"; // PAN
                AddField("PIN:", txtParam1);
                AddField("Account (PAN):", txtParam2);
                break;
            case "Verify PIN":
                txtParam1.Text = "1234";
                txtParam2.Text = "4000000000000002";
                AddField("PIN:", txtParam1);
                AddField("Account (PAN):", txtParam2);
                break;
            default:
                break;
        }

        actionDetails.Controls.Add(fl);
    }

    // Payload construction moved to PayloadBuilder.cs

    private async Task SendActionAsync()
    {
        btnSend.Enabled = false;
        txtResponse.Clear();
        string host = txtHost.Text.Trim();
        int port = (int)numPort.Value;
        string actionKey = cmbAction.SelectedItem as string;
        if (string.IsNullOrEmpty(actionKey)) { btnSend.Enabled = true; return; }
        string payload = PayloadBuilder.BuildPayload(actionKey, txtParam1.Text, txtParam2.Text, chkFlag.Checked);
        if (string.IsNullOrEmpty(payload)) { txtResponse.Text = "No payload constructed for action."; btnSend.Enabled = true; return; }

        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port);
            using var stream = tcp.GetStream();
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
