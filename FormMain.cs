using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace ProThailand
{
    public class FormMain : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtServerUrl;
        private RadioButton rbLocalhost;
        private RadioButton rbWorker;
        private RadioButton rbProThailand;
        private Button btnLogin;
        private Button btnInspectHwid;
        private Button btnLaunch;
        private Label lblStatus;
        private TextBox txtLogs;

        public FormMain()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "ProThailand Launcher (LFS 0.6V)";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(24, 28, 36);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Title Banner
            Label lblTitle = new Label
            {
                Text = "PROTHAILAND LAUNCHER",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(24, 18),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Live for Speed 0.6V Master Authentication & Hardware Verification",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(26, 50),
                AutoSize = true
            };
            this.Controls.Add(lblSubtitle);

            // Group: Server Target
            GroupBox gbServer = new GroupBox
            {
                Text = " Master Server Endpoint ",
                Location = new Point(24, 80),
                Size = new Size(636, 125),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            rbWorker = new RadioButton
            {
                Text = "Cloudflare Worker (https://prothailand-master-api.fixflxw24.workers.dev/api.php)",
                Location = new Point(20, 24),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(251, 146, 60) // Orange/Cloudflare
            };
            rbWorker.CheckedChanged += ServerRadio_CheckedChanged;
            gbServer.Controls.Add(rbWorker);

            rbLocalhost = new RadioButton
            {
                Text = "Localhost Test Server (http://localhost:8080/api.php)",
                Location = new Point(20, 46),
                AutoSize = true,
                ForeColor = Color.FromArgb(74, 222, 128) // Green
            };
            rbLocalhost.CheckedChanged += ServerRadio_CheckedChanged;
            gbServer.Controls.Add(rbLocalhost);

            rbProThailand = new RadioButton
            {
                Text = "Production Custom Domain (https://api.prothailand.net/api.php)",
                Location = new Point(20, 68),
                AutoSize = true,
                ForeColor = Color.FromArgb(56, 189, 248) // Blue
            };
            rbProThailand.CheckedChanged += ServerRadio_CheckedChanged;
            gbServer.Controls.Add(rbProThailand);

            txtServerUrl = new TextBox
            {
                Location = new Point(20, 92),
                Size = new Size(596, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "https://prothailand-master-api.fixflxw24.workers.dev/api.php"
            };
            gbServer.Controls.Add(txtServerUrl);
            this.Controls.Add(gbServer);

            // Group: Login Credentials
            GroupBox gbAuth = new GroupBox
            {
                Text = " User Authentication ",
                Location = new Point(24, 215),
                Size = new Size(636, 110),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            Label lblUser = new Label { Text = "Username:", Location = new Point(20, 30), AutoSize = true };
            gbAuth.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(100, 27),
                Size = new Size(210, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Pilot01"
            };
            gbAuth.Controls.Add(txtUsername);

            Label lblPass = new Label { Text = "Password:", Location = new Point(330, 30), AutoSize = true };
            gbAuth.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(405, 27),
                Size = new Size(211, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '•',
                Text = "123456"
            };
            gbAuth.Controls.Add(txtPassword);

            btnInspectHwid = new Button
            {
                Text = "🔍 Inspect HWID Fingerprint",
                Location = new Point(20, 65),
                Size = new Size(200, 32),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnInspectHwid.FlatAppearance.BorderSize = 0;
            btnInspectHwid.Click += BtnInspectHwid_Click;
            gbAuth.Controls.Add(btnInspectHwid);

            btnLogin = new Button
            {
                Text = "🚀 Login & Authenticate",
                Location = new Point(230, 65),
                Size = new Size(185, 32),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            gbAuth.Controls.Add(btnLogin);

            btnLaunch = new Button
            {
                Text = "🏁 Launch LFS.exe",
                Location = new Point(425, 65),
                Size = new Size(191, 32),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += BtnLaunch_Click;
            gbAuth.Controls.Add(btnLaunch);

            this.Controls.Add(gbAuth);

            // Status Badge
            lblStatus = new Label
            {
                Text = "Status: Ready for authentication.",
                Location = new Point(26, 335),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225)
            };
            this.Controls.Add(lblStatus);

            // Log Console
            txtLogs = new TextBox
            {
                Location = new Point(24, 365),
                Size = new Size(636, 175),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(11, 15, 25),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtLogs);

            Log("ProThailand Launcher initialized.");
            Log("Active endpoint: " + txtServerUrl.Text);
            CheckLfsStatus();
        }

        private void ServerRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (rbWorker.Checked)
                txtServerUrl.Text = "https://prothailand-master-api.fixflxw24.workers.dev/api.php";
            else if (rbLocalhost.Checked)
                txtServerUrl.Text = LoginHandle.DefaultLocalServer;
            else if (rbProThailand.Checked)
                txtServerUrl.Text = LoginHandle.DefaultRemoteServer;
        }

        private void CheckLfsStatus()
        {
            string lfsPath = LFSManager.FindLfsExecutable();
            if (!string.IsNullOrEmpty(lfsPath))
            {
                Log("Detected LFS.exe at: " + lfsPath);
            }
            else
            {
                Log("Warning: LFS.exe not detected in search paths.");
            }
        }

        private void BtnInspectHwid_Click(object sender, EventArgs e)
        {
            string mac = HardwareInfo.GetMacAddress();
            string mb = HardwareInfo.GetMotherboardSerial();
            string bios = HardwareInfo.GetBiosUuid();
            string hdd = HardwareInfo.GetHddSerial();
            string payload = HardwareInfo.BuildPayloadString(txtUsername.Text, txtPassword.Text);

            string info = string.Format(
                "=== HARDWARE FINGERPRINT (HWID) ===\n\n" +
                "• MAC Address:       {0}\n" +
                "• Motherboard Serial: {1}\n" +
                "• BIOS UUID:          {2}\n" +
                "• Disk Model/Serial:  {3}\n" +
                "• OS User / Machine:  {4} @ {5}\n\n" +
                "=== UNENCRYPTED PAYLOAD (Sent to Master Server) ===\n{6}",
                mac, mb, bios, hdd, Environment.UserName, Environment.MachineName, payload);

            Log("Inspected HWID: MAC=" + mac + ", MB=" + mb + ", BIOS=" + bios);
            MessageBox.Show(info, "Hardware Fingerprint Inspection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string serverUrl = txtServerUrl.Text.Trim();

            lblStatus.Text = "Status: Authenticating with " + serverUrl + "...";
            lblStatus.ForeColor = Color.Yellow;
            btnLogin.Enabled = false;

            ThreadPool.QueueUserWorkItem(state =>
            {
                Log("Connecting to Master Server: " + serverUrl);
                var res = LoginHandle.DoLogin(user, pass, serverUrl);

                this.Invoke((MethodInvoker)delegate
                {
                    btnLogin.Enabled = true;
                    if (res.Success)
                    {
                        lblStatus.Text = "Status: " + res.Message;
                        lblStatus.ForeColor = Color.FromArgb(74, 222, 128); // Green
                        Log("[+] SUCCESS: " + res.RawResponse);
                    }
                    else
                    {
                        lblStatus.Text = "Status: " + res.Message;
                        lblStatus.ForeColor = Color.FromArgb(248, 113, 113); // Red
                        Log("[-] FAILED (" + res.Status + "): " + res.Message);
                        if (!string.IsNullOrEmpty(res.RawResponse))
                            Log("    Server raw response: " + res.RawResponse);
                    }
                });
            });
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            string lfsPath = LFSManager.FindLfsExecutable();
            if (string.IsNullOrEmpty(lfsPath))
            {
                MessageBox.Show("Could not find LFS.exe in application directory or standard locations.",
                    "LFS Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;

            Log("Starting LFS: " + lfsPath);
            Log(string.Format("Injecting Web Credentials -> User: '{0}', usemaster=yes", string.IsNullOrEmpty(user) ? "guest" : user));
            if (LFSManager.LaunchLfs(lfsPath, user, pass, "127.0.0.1"))
            {
                Log("[+] LFS.exe process launched successfully with Web usemaster!");
            }
            else
            {
                Log("[-] Failed to launch LFS.exe process.");
            }
        }

        public void Log(string msg)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText(string.Format("[{0}] {1}\r\n", timeStamp, msg));
        }
    }
}
