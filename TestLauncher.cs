using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace ProThailand
{
    class TestLauncher
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ProThailand LFS Launcher Test ===");
            Console.WriteLine("Testing player profile generation and memory injection...\n");

            // Test parameters
            string testUsername = "testplayer";
            string testPassword = "testpass123";
            string lfsPath = "/mnt/sdcard/LFS_0.6R_S2/LFS.exe";

            // 1. Check if LFS.exe exists
            if (!File.Exists(lfsPath))
            {
                Console.WriteLine($"❌ LFS.exe not found at: {lfsPath}");
                Console.WriteLine("Looking for LFS.exe in common locations...");
                
                string foundPath = LFSManager.FindLfsExecutable();
                if (foundPath == null)
                {
                    Console.WriteLine("❌ Could not find LFS.exe. Please specify path manually.");
                    return;
                }
                
                lfsPath = foundPath;
                Console.WriteLine($"✓ Found LFS.exe at: {lfsPath}");
            }
            else
            {
                Console.WriteLine($"✓ LFS.exe found at: {lfsPath}");
            }

            string lfsDir = Path.GetDirectoryName(lfsPath);
            Console.WriteLine($"LFS Directory: {lfsDir}\n");

            // 2. Test profile generation
            Console.WriteLine("=== Testing Player Profile Generation ===");
            bool profileCreated = LFSManager.SetupPlayerProfile(lfsDir, testUsername);
            Console.WriteLine($"Player profile generation: {(profileCreated ? "✓ SUCCESS" : "✗ FAILED")}");

            // 3. Test configuration update
            Console.WriteLine("\n=== Testing LFS Configuration ===");
            LFSManager.UpdateLfsConfig(lfsDir, testUsername, testPassword);
            
            string cfgPath = Path.Combine(lfsDir, "cfg.txt");
            if (File.Exists(cfgPath))
            {
                string cfgContent = File.ReadAllText(cfgPath);
                bool hasUsername = cfgContent.Contains($"user_name {testUsername}");
                bool hasUsemaster = cfgContent.Contains("usemaster yes");
                
                Console.WriteLine($"Config file exists: ✓");
                Console.WriteLine($"Has username: {(hasUsername ? "✓" : "✗")}");
                Console.WriteLine($"Has usemaster: {(hasUsemaster ? "✓" : "✗")}");
            }
            else
            {
                Console.WriteLine("Config file: ✗ NOT FOUND");
            }

            // 4. Check profile files
            Console.WriteLine("\n=== Checking Generated Files ===");
            string miscDir = Path.Combine(lfsDir, "data", "misc");
            
            string plyPath = Path.Combine(miscDir, $"{testUsername}.ply");
            Console.WriteLine($"Player profile (.ply): {(File.Exists(plyPath) ? "✓ EXISTS" : "✗ MISSING")}");
            
            string usrPath = Path.Combine(miscDir, "usr.bin");
            Console.WriteLine($"User database (usr.bin): {(File.Exists(usrPath) ? "✓ EXISTS" : "✗ MISSING")}");
            
            string drvPath = Path.Combine(miscDir, "drv.nam");
            Console.WriteLine($"Driver name (drv.nam): {(File.Exists(drvPath) ? "✓ EXISTS" : "✗ MISSING")}");

            // 5. Test memory injection (simulated - would need actual process)
            Console.WriteLine("\n=== Memory Injection Test (Simulation) ===");
            Console.WriteLine("The following memory offsets will be injected:");
            Console.WriteLine($"  • Master Username Buffer: 0x9B0AA4");
            Console.WriteLine($"  • Active Player Profile: 0x7B9CF4");
            Console.WriteLine($"  • In-game Display Name: 0x629214");
            Console.WriteLine($"  • Alternative In-game Name: 0x229214");
            Console.WriteLine($"  • Currently Loaded Profile: 0x3B9CF4");
            Console.WriteLine($"  • Master Password Buffer: 0x9B0A8C");
            Console.WriteLine($"  • Master Server Redirect: 0x5C890C → 127.0.0.1");

            // 6. Test launch capability
            Console.WriteLine("\n=== Launch Test ===");
            Console.WriteLine("Note: This would normally launch LFS.exe with injection.");
            Console.WriteLine("For safety, we'll simulate without actual launch.");
            
            Console.Write("\nDo you want to test actual launch? (y/N): ");
            string response = Console.ReadLine()?.ToLower();
            
            if (response == "y" || response == "yes")
            {
                Console.WriteLine("\nLaunching LFS.exe with full injection...");
                
                // Start LocalMasterBridge first (in background)
                Console.WriteLine("Starting LocalMasterBridge on port 29339...");
                // (LocalMasterBridge would start here)
                
                // Launch LFS
                Process lfsProcess = LFSManager.LaunchLfsProcess(lfsPath, testUsername, testPassword, "127.0.0.1");
                
                if (lfsProcess != null)
                {
                    Console.WriteLine($"✓ LFS launched (PID: {lfsProcess.Id})");
                    Console.WriteLine("Memory injection scheduled with delays: 50ms, 150ms, 400ms, 800ms, 1200ms");
                    
                    // Wait a bit to see if process stays alive
                    Thread.Sleep(2000);
                    
                    if (!lfsProcess.HasExited)
                    {
                        Console.WriteLine("✓ LFS process still running");
                        Console.WriteLine("Verifying memory injection...");
                        
                        // We would verify injection here if we had the process handle
                        Console.WriteLine("Note: Verification requires process to be fully loaded.");
                        
                        Console.Write("\nPress Enter to terminate LFS process...");
                        Console.ReadLine();
                        
                        lfsProcess.Kill();
                        Console.WriteLine("LFS process terminated.");
                    }
                    else
                    {
                        Console.WriteLine("✗ LFS process exited prematurely");
                    }
                }
                else
                {
                    Console.WriteLine("✗ Failed to launch LFS");
                }
            }
            else
            {
                Console.WriteLine("\nLaunch test skipped.");
            }

            // 7. Summary
            Console.WriteLine("\n=== TEST SUMMARY ===");
            Console.WriteLine("To complete the setup, ensure the following:");
            Console.WriteLine("1. LocalMasterBridge.cs is running on 127.0.0.1:29339");
            Console.WriteLine("2. Cloudflare Worker API is accessible");
            Console.WriteLine("3. LFS.exe is pre-patched (m.lfspro.net → 127.0.0.1)");
            Console.WriteLine("4. Player clicks 'Unlock' in-game OR auto-auth is triggered");
            Console.WriteLine("\nExpected result:");
            Console.WriteLine("- Username appears correctly in-game");
            Console.WriteLine("- No 'unnamed' player");
            Console.WriteLine("- Auto S3 license authorization");
            Console.WriteLine("- No connection to external lfs.net");

            Console.WriteLine("\nTest completed.");
        }
    }
}
