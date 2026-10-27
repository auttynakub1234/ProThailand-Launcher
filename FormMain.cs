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
            this.BackColor = Color.FromArgb(24, 28, 36);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f);

            // Title
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
                Text = "Live for Speed 0.6V - Web Authentication + Master Bridge",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(26, 50),
                AutoSize = true
            };
            this.Controls.Add(lblSubtitle);

            // Server selection
            GroupBox gbServer = new GroupBox
            {
                Text = " Web Authentication Server ",
                Location = new Point(24, 80),
                Size = new Size(636, 100),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            rbWorker = new RadioButton
            {
                Text = "Cloudflare Worker (prothailand-master-api.fixflxw24.workers.dev)",
                Location = new Point(20, 24),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(251, 146, 60)
            };
            gbServer.Controls.Add(rbWorker);

            rbLocalhost = new RadioButton
            {
                Text = "Localhost Test Server (localhost:8080)",
                Location = new Point(20, 48),
                AutoSize = true,
                ForeColor = Color.FromArgb(74, 222, 128)
            };
            gbServer.Controls.Add(rbLocalhost);

            rbProThailand = new RadioButton
            {
                Text = "Production (prothailand.net)",
                Location = new Point(20, 72),
                AutoSize = true,
                ForeColor = Color.FromArgb(56, 189, 248)
            };
            gbServer.Controls.Add(rbProThailand);

            this.Controls.Add(gbServer);

            // Username
            Label lblUser = new Label
            {
                Text = "Username:",
                Location = new Point(24, 190),
                AutoSize = true
            };
            this.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(100, 187),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Pilot01"
            };
            this.Controls.Add(txtUsername);

            // Password
            Label lblPass = new Label
            {
                Text = "Password:",
                Location = new Point(320, 190),
                AutoSize = true
            };
            this.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(400, 187),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '*',
                Text = "123456"
            };
            this.Controls.Add(txtPassword);

            // Buttons
            btnInspectHwid = new Button
            {
                Text = "🔍 Inspect HWID",
                Location = new Point(24, 230),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnInspectHwid.Click += BtnInspectHwid_Click;
            this.Controls.Add(btnInspectHwid);

            btnLogin = new Button
            {
                Text = "🚀 Login & Start Bridge",
                Location = new Point(220, 230),
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            btnLaunch = new Button
            {
                Text = "🏁 Launch LFS.exe",
                Location = new Point(436, 230),
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLaunch.Click += BtnLaunch_Click;
            this.Controls.Add(btnLaunch);

            // Status label
            lblStatus = new Label
            {
                Text = "Status: Ready - Web Auth + Master Bridge",
                Location = new Point(24, 280),
                AutoSize = true,
                ForeColor = Color.White
            };
            this.Controls.Add(lblStatus);

            // Info about hosts file
            Label lblHostsInfo = new Label
            {
                Text = "⚠️ Add to C:\\Windows\\System32\\drivers\\etc\\hosts: 127.0.0.1 m.lfspro.net",
                Location = new Point(24, 305),
                AutoSize = true,
                ForeColor = Color.FromArgb(252, 211, 77)
            };
            this.Controls.Add(lblHostsInfo);

            // Logs
            txtLogs = new TextBox
            {
                Location = new Point(24, 330),
                Size = new Size(632, 220),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            this.Controls.Add(txtLogs);

            Log("ProThailand Launcher initialized.");
            Log("Mode: Web Authentication + LFS Master Bridge");
            CheckLfsStatus();
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
                "• Disk Model/Serial:  {3}\n\n" +
                "=== UNENCRYPTED PAYLOAD (Sent to Web Server) ===\n{4}",
                mac, mb, bios, hdd, payload);

            Log("Inspected HWID: MAC=" + mac + ", MB=" + mb + ", BIOS=" + bios);
            MessageBox.Show(info, "Hardware Fingerprint Inspection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string serverUrl = GetSelectedServerUrl();

            lblStatus.Text = "Status: Authenticating with Web Server...";
            lblStatus.ForeColor = Color.Yellow;
            btnLogin.Enabled = false;
            btnLaunch.Enabled = false;

            ThreadPool.QueueUserWorkItem(state =>
            {
                Log("Connecting to Web Auth Server: " + serverUrl);
                var res = LoginHandle.DoLogin(user, pass, serverUrl);

                this.Invoke((MethodInvoker)delegate
                {
                    btnLogin.Enabled = true;
                    
                    if (res.Success)
                    {
                        lblStatus.Text = "Status: " + res.Message + " - Starting Master Bridge";
                        lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                        Log("[+] SUCCESS: " + res.Message);
                        Log("[+] User verified in ProThailand database");
                        
                        // Stop existing bridge if any
                        if (LocalMasterBridge.IsRunning)
                        {
                            LocalMasterBridge.Stop();
                            Thread.Sleep(200);
                        }
                        
                        // Start Local Master Bridge
                        try
                        {
                            LocalMasterBridge.Start(serverUrl);
                            Log("[*] Local Master Bridge started on 127.0.0.1:29339");
                            Log("[*] LFS will connect to m.lfspro.net -> 127.0.0.1:29339");
                            Log("[*] Bridge will verify user: " + user);
                            
                            btnLaunch.Enabled = true;
                            lblStatus.Text = "Status: Ready - Web Auth OK, Bridge Running";
                        }
                        catch (Exception ex)
                        {
                            Log("[-] Bridge failed: " + ex.Message);
                            lblStatus.Text = "Status: Web Auth OK, Bridge Failed";
                        }
                    }
                    else
                    {
                        lblStatus.Text = "Status: " + res.Message;
                        lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                        Log("[-] FAILED (" + res.Status + "): " + res.Message);
                        btnLaunch.Enabled = false;
                    }
                });
            });
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            if (!LocalMasterBridge.IsRunning)
            {
                MessageBox.Show("Master Bridge not running. Please login first.",
                    "Bridge Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
            Log("[*] Web User: " + user);
            Log("[*] Master Bridge: 127.0.0.1:29339 (m.lfspro.net)");
            Log("[*] Injecting credentials into LFS memory...");
            
            if (LFSManager.LaunchLfs(lfsPath, user, pass, "127.0.0.1"))
            {
                Log("[+] LFS.exe launched with credentials injected");
                Log("[+] LFS will connect to local bridge -> Web Auth Server");
            }
            else
            {
                Log("[-] Failed to launch LFS.exe");
            }
        }

        private string GetSelectedServerUrl()
        {
            if (rbProThailand.Checked)
                return "https://prothailand.net/api.php";
            else if (rbLocalhost.Checked)
                return "http://localhost:8080/api.php";
            else // rbWorker.Checked
                return "https://prothailand-master-api.fixflxw24.workers.dev/api.php";
        }

        public void Log(string msg)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText(string.Format("[{0}] {1}\r\n", timeStamp, msg));
        }
    }
}
