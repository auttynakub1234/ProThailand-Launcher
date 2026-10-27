using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ProThailand
{
    public class FormMain : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtLfsPath;
        private Button btnBrowse;
        private RadioButton rbLocalhost;
        private RadioButton rbWorker;
        private RadioButton rbProThailand;
        private Button btnQuickStart;
        private Button btnLogin;
        private Button btnInspectHwid;
        private Button btnFixHosts;
        private CheckBox chkAutoLaunch;
        private Label lblStatus;
        private TextBox txtLogs;
        private System.Windows.Forms.Timer tmrAutoStart;
        private int autoStartCountdown = 3;

        public FormMain()
        {
            InitializeComponent();
            LocalMasterBridge.OnLog += msg =>
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate { Log(msg); });
                }
            };
        }

        private void InitializeComponent()
        {
            this.Text = "ProThailand Launcher (LFS 0.6V) — Auto Login & Master Bridge";
            this.Size = new Size(740, 660);
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
                Location = new Point(24, 16),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Live for Speed 0.6V — One-Click Auto Login + Local Master Bridge (S3 License)",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(26, 48),
                AutoSize = true
            };
            this.Controls.Add(lblSubtitle);

            // Server selection GroupBox
            GroupBox gbServer = new GroupBox
            {
                Text = " Web Authentication Server ",
                Location = new Point(24, 75),
                Size = new Size(676, 85),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            rbWorker = new RadioButton
            {
                Text = "Cloudflare Worker (prothailand-master-api.fixflxw24.workers.dev)",
                Location = new Point(16, 22),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.FromArgb(251, 146, 60)
            };
            gbServer.Controls.Add(rbWorker);

            rbLocalhost = new RadioButton
            {
                Text = "Localhost Test Server (localhost:8080)",
                Location = new Point(16, 48),
                AutoSize = true,
                ForeColor = Color.FromArgb(74, 222, 128)
            };
            gbServer.Controls.Add(rbLocalhost);

            rbProThailand = new RadioButton
            {
                Text = "Production (prothailand.net)",
                Location = new Point(340, 48),
                AutoSize = true,
                ForeColor = Color.FromArgb(56, 189, 248)
            };
            gbServer.Controls.Add(rbProThailand);

            this.Controls.Add(gbServer);

            // Username
            Label lblUser = new Label
            {
                Text = "Username:",
                Location = new Point(24, 172),
                AutoSize = true
            };
            this.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Location = new Point(100, 169),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = LFSManager.GetSetting("Username", "Pilot01")
            };
            this.Controls.Add(txtUsername);

            // Password
            Label lblPass = new Label
            {
                Text = "Password:",
                Location = new Point(320, 172),
                AutoSize = true
            };
            this.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Location = new Point(400, 169),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '*',
                Text = LFSManager.GetSetting("Password", "123456")
            };
            this.Controls.Add(txtPassword);

            // LFS.exe Path row
            Label lblLfs = new Label
            {
                Text = "LFS Path:",
                Location = new Point(24, 206),
                AutoSize = true
            };
            this.Controls.Add(lblLfs);

            txtLfsPath = new TextBox
            {
                Location = new Point(100, 203),
                Size = new Size(480, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = false
            };
            this.Controls.Add(txtLfsPath);

            btnBrowse = new Button
            {
                Text = "📁 Browse...",
                Location = new Point(590, 202),
                Size = new Size(110, 26),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // Action Buttons
            btnQuickStart = new Button
            {
                Text = "🏁 เริ่มเกม LFS ทันที (Auto Login & Launch)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(24, 242),
                Size = new Size(390, 42),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnQuickStart.FlatAppearance.BorderSize = 0;
            btnQuickStart.Click += BtnQuickStart_Click;
            this.Controls.Add(btnQuickStart);

            btnLogin = new Button
            {
                Text = "🚀 Login Web Only",
                Location = new Point(424, 242),
                Size = new Size(155, 42),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            btnInspectHwid = new Button
            {
                Text = "🔍 HWID",
                Location = new Point(590, 242),
                Size = new Size(110, 42),
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnInspectHwid.Click += BtnInspectHwid_Click;
            this.Controls.Add(btnInspectHwid);

            // Auto-launch checkbox
            chkAutoLaunch = new CheckBox
            {
                Text = "⚡ เริ่มเกมอัตโนมัติทันทีเมื่อเปิด Launcher (เปิดไฟล์เดียวเข้าเกมเลย)",
                Location = new Point(26, 294),
                AutoSize = true,
                ForeColor = Color.FromArgb(203, 213, 225),
                Checked = (LFSManager.GetSetting("AutoLaunch", "0") == "1")
            };
            chkAutoLaunch.CheckedChanged += (s, e) =>
            {
                LFSManager.SaveSetting("AutoLaunch", chkAutoLaunch.Checked ? "1" : "0");
                if (!chkAutoLaunch.Checked && tmrAutoStart != null)
                {
                    tmrAutoStart.Stop();
                    lblStatus.Text = "Status: พร้อมใช้งาน (ยกเลิกการเริ่มอัตโนมัติแล้ว)";
                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                }
            };
            this.Controls.Add(chkAutoLaunch);

            // Status label & Hosts button
            lblStatus = new Label
            {
                Text = "Status: พร้อมใช้งาน (Ready) — กดปุ่มเริ่มเกมเพื่อเข้าเล่นได้ทันที",
                Location = new Point(24, 322),
                AutoSize = true,
                ForeColor = Color.FromArgb(74, 222, 128)
            };
            this.Controls.Add(lblStatus);

            btnFixHosts = new Button
            {
                Text = "🛠️ Auto Hosts",
                Location = new Point(590, 317),
                Size = new Size(110, 25),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(203, 213, 225),
                FlatStyle = FlatStyle.Flat
            };
            btnFixHosts.Click += (s, e) =>
            {
                if (LFSManager.TryAddHostsEntry())
                {
                    Log("[+] Hosts file checked: 127.0.0.1 m.lfspro.net is configured.");
                    MessageBox.Show("บันทึกการตั้งค่า Hosts file สำเร็จ!", "Hosts Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Log("[!] Could not edit hosts file automatically (Run launcher as Administrator if needed).");
                    MessageBox.Show("ไม่สามารถแก้ไข hosts file ได้โดยอัตโนมัติ (กรุณาคลิกขวาแล้ว Run as Administrator หรือเพิ่มบรรทัด: 127.0.0.1 m.lfspro.net ด้วยตนเอง)", "Hosts Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            this.Controls.Add(btnFixHosts);

            // Logs text box
            txtLogs = new TextBox
            {
                Location = new Point(24, 348),
                Size = new Size(676, 250),
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
            Log("Mode: One-Click Quick Start (Auto Login + Local Master Bridge)");

            // Auto-detect LFS path
            string detectedLfs = LFSManager.FindLfsExecutable();
            if (!string.IsNullOrEmpty(detectedLfs))
            {
                txtLfsPath.Text = detectedLfs;
                Log("Detected LFS.exe at: " + detectedLfs);
            }
            else
            {
                Log("Notice: LFS.exe not automatically found. Please click 'Browse' to select your LFS.exe");
            }

            // If auto-launch is enabled, start countdown
            if (chkAutoLaunch.Checked && !string.IsNullOrEmpty(txtLfsPath.Text) && File.Exists(txtLfsPath.Text))
            {
                autoStartCountdown = 2;
                lblStatus.Text = string.Format("Status: กำลังเริ่มเกมอัตโนมัติใน {0} วินาที... (คลิกยกเลิกได้ที่ช่องสี่เหลี่ยม)", autoStartCountdown);
                lblStatus.ForeColor = Color.FromArgb(251, 146, 60);

                tmrAutoStart = new System.Windows.Forms.Timer();
                tmrAutoStart.Interval = 1000;
                tmrAutoStart.Tick += (s, e) =>
                {
                    autoStartCountdown--;
                    if (autoStartCountdown > 0)
                    {
                        lblStatus.Text = string.Format("Status: กำลังเริ่มเกมอัตโนมัติใน {0} วินาที... (คลิกยกเลิกได้ที่ช่องสี่เหลี่ยม)", autoStartCountdown);
                    }
                    else
                    {
                        tmrAutoStart.Stop();
                        BtnQuickStart_Click(null, null);
                    }
                };
                tmrAutoStart.Start();
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            if (tmrAutoStart != null) tmrAutoStart.Stop();

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "เลือกไฟล์เกม LFS.exe";
                ofd.Filter = "LFS Executable (LFS.exe)|LFS.exe|All Executables (*.exe)|*.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtLfsPath.Text = ofd.FileName;
                    LFSManager.CustomLfsPath = ofd.FileName;
                    Log("[+] Selected LFS path: " + ofd.FileName);
                }
            }
        }

        private void BtnQuickStart_Click(object sender, EventArgs e)
        {
            if (tmrAutoStart != null) tmrAutoStart.Stop();

            string lfsPath = txtLfsPath.Text.Trim();
            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
            {
                lfsPath = LFSManager.FindLfsExecutable();
                if (!string.IsNullOrEmpty(lfsPath))
                    txtLfsPath.Text = lfsPath;
            }

            if (string.IsNullOrEmpty(lfsPath) || !File.Exists(lfsPath))
            {
                MessageBox.Show("ไม่พบไฟล์ LFS.exe กรุณากดปุ่ม 'Browse...' เพื่อเลือกตำแหน่งของตัวเกม LFS.exe บนเครื่องของคุณ",
                    "ไม่พบไฟล์ LFS.exe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BtnBrowse_Click(sender, e);
                return;
            }

            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("กรุณากรอก Username และ Password ให้ครบถ้วน", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save credentials
            LFSManager.SaveSetting("Username", user);
            LFSManager.SaveSetting("Password", pass);

            string serverUrl = GetSelectedServerUrl();
            lblStatus.Text = "Status: กำลังเชื่อมต่อ Master Bridge & เริ่มเกม LFS...";
            lblStatus.ForeColor = Color.Yellow;
            btnQuickStart.Enabled = false;

            // 1. Ensure Local Master Bridge is running with active user
            try
            {
                LocalMasterBridge.ActiveUsername = user;
                if (!LocalMasterBridge.IsRunning)
                {
                    LocalMasterBridge.Start(serverUrl, user);
                    Log("[*] Local Master Bridge started on 127.0.0.1:29339");
                }
            }
            catch (Exception ex)
            {
                Log("[-] Bridge Start Warning: " + ex.Message);
            }

            // 2. Perform authentication and launch in background thread
            ThreadPool.QueueUserWorkItem(state =>
            {
                Log("Authenticating user '" + user + "' with Web Server...");
                var loginRes = LoginHandle.DoLogin(user, pass, serverUrl);

                if (loginRes.Success)
                {
                    Log("[+] Web Auth: " + loginRes.Message);
                }
                else
                {
                    Log("[!] Web Auth Notice (" + loginRes.Status + "): " + loginRes.Message);
                    Log("[*] Proceeding with Local Master Bridge active session for '" + user + "'");
                }

                Log("[*] Launching LFS.exe: " + lfsPath);
                Process proc = LFSManager.LaunchLfsProcess(lfsPath, user, pass, "127.0.0.1");

                this.BeginInvoke((MethodInvoker)delegate
                {
                    btnQuickStart.Enabled = true;
                    if (proc != null)
                    {
                        lblStatus.Text = "Status: ตัวเกม LFS เปิดแล้ว! (กำลังเชื่อมต่อไปยัง Master Bridge)";
                        lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                        Log("[+] SUCCESS: LFS.exe launched successfully with credentials injected.");

                        // If auto-launch enabled, minimize launcher and exit when LFS quits
                        if (chkAutoLaunch.Checked)
                        {
                            this.WindowState = FormWindowState.Minimized;
                            ThreadPool.QueueUserWorkItem(w =>
                            {
                                try
                                {
                                    proc.WaitForExit();
                                    Thread.Sleep(300);
                                    LocalMasterBridge.Stop();
                                    Application.Exit();
                                }
                                catch { }
                            });
                        }
                    }
                    else
                    {
                        lblStatus.Text = "Status: ไม่สามารถเปิด LFS.exe ได้";
                        lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                        Log("[-] FAILED: Could not start LFS.exe. Please verify path and permissions.");
                    }
                });
            });
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (tmrAutoStart != null) tmrAutoStart.Stop();

            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string serverUrl = GetSelectedServerUrl();

            lblStatus.Text = "Status: Authenticating with Web Server...";
            lblStatus.ForeColor = Color.Yellow;
            btnLogin.Enabled = false;

            ThreadPool.QueueUserWorkItem(state =>
            {
                Log("Connecting to Web Auth Server: " + serverUrl);
                var res = LoginHandle.DoLogin(user, pass, serverUrl);

                this.BeginInvoke((MethodInvoker)delegate
                {
                    btnLogin.Enabled = true;
                    if (res.Success)
                    {
                        lblStatus.Text = "Status: " + res.Message;
                        lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                        Log("[+] SUCCESS: " + res.Message);
                        LocalMasterBridge.Start(serverUrl, user);
                    }
                    else
                    {
                        lblStatus.Text = "Status: " + res.Message;
                        lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                        Log("[-] FAILED (" + res.Status + "): " + res.Message);
                    }
                });
            });
        }

        private void BtnInspectHwid_Click(object sender, EventArgs e)
        {
            if (tmrAutoStart != null) tmrAutoStart.Stop();

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
