#!/bin/bash
echo "=== Compiling ProThailand Launcher ==="
echo "Target: ProThailand.exe with enhanced memory injection"

# Check for mono compiler
if ! command -v mcs &> /dev/null; then
    echo "ERROR: mcs (Mono C# Compiler) not found"
    echo "Installing mono-devel package..."
    apt-get update && apt-get install -y mono-devel || {
        echo "Failed to install mono. Please install manually."
        exit 1
    }
fi

# Create backup of original files
echo "Creating backups..."
cp -f LFSManager.cs LFSManager.cs.backup 2>/dev/null || true
cp -f LFSInjector.cs LFSInjector.cs.backup 2>/dev/null || true

# Copy updated files
echo "Copying updated implementations..."
cp -f UpdatedLFSManager.cs LFSManager.cs
cp -f EnhancedLFSInjector.cs LFSInjector.cs

# Compile the main launcher
echo "Compiling ProThailand.exe..."
mcs -target:winexe \
    -platform:anycpu \
    -sdk:4.5 \
    -out:ProThailand.exe \
    -reference:System.Windows.Forms.dll \
    -reference:System.Drawing.dll \
    -reference:System.Management.dll \
    -reference:System.Core.dll \
    -reference:System.Net.Http.dll \
    FormMain.cs \
    HardwareInfo.cs \
    LFSInjector.cs \
    LFSManager.cs \
    LocalMasterBridge.cs \
    LoginHandle.cs \
    Program.cs \
    SecureCrypto.cs

if [ $? -eq 0 ]; then
    echo "✓ Compilation successful!"
    ls -lh ProThailand.exe
    
    # Create config file
    echo "Creating ProThailand.exe.config..."
    cat > ProThailand.exe.config << 'CONFIG'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0.30319" />
    <supportedRuntime version="v2.0.50727" />
  </startup>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Net.Http" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.2.0.0" newVersion="4.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
CONFIG
    
    echo "✓ Configuration file created"
    
    # Test compile the test launcher
    echo "Compiling TestLauncher.exe..."
    mcs -target:exe \
        -platform:anycpu \
        -sdk:4.5 \
        -out:TestLauncher.exe \
        -reference:System.Windows.Forms.dll \
        -reference:System.Drawing.dll \
        -reference:System.Management.dll \
        TestLauncher.cs \
        LFSInjector.cs \
        LFSManager.cs \
        SecureCrypto.cs
    
    if [ $? -eq 0 ]; then
        echo "✓ TestLauncher.exe compiled successfully"
    fi
    
    # Create deployment package
    echo "Creating deployment package..."
    mkdir -p ProThailand-Deploy
    cp -f ProThailand.exe ProThailand.exe.config Play.bat ProThailand-Deploy/
    cp -f lfs_master_server.py run_master_server.bat ProThailand-Deploy/
    cp -f worker.js ProThailand-Deploy/
    cp -f README.md README-SYSTEM.md ProThailand-Deploy/
    
    # Create zip archive
    cd ProThailand-Deploy
    zip -r ../ProThailand-Complete.zip *
    cd ..
    
    echo "✓ Deployment package created: ProThailand-Complete.zip"
    echo "✓ Files included: ProThailand.exe, config, master server, documentation"
    
else
    echo "✗ Compilation failed!"
    echo "Restoring original files..."
    cp -f LFSManager.cs.backup LFSManager.cs 2>/dev/null || true
    cp -f LFSInjector.cs.backup LFSInjector.cs 2>/dev/null || true
    exit 1
fi

echo ""
echo "=== COMPILATION COMPLETE ==="
echo "To run: mono ProThailand.exe"
echo "Or double-click ProThailand.exe on Windows"
echo ""
echo "Features implemented:"
echo "1. ✓ Player profile generation (.ply, usr.bin, drv.nam)"
echo "2. ✓ Memory injection for in-game username display"
echo "3. ✓ Multiple buffer injection (0x7B9CF4, 0x629214, etc.)"
echo "4. ✓ Master server redirection (m.lfspro.net → 127.0.0.1)"
echo "5. ✓ Winsock connection forcing"
echo "6. ✓ Config file management (cfg.txt)"
echo ""
echo "Next steps:"
echo "1. Ensure LFS.exe is pre-patched (m.lfspro.net → 127.0.0.1)"
echo "2. Run LocalMasterBridge on port 29339"
echo "3. Configure Cloudflare Worker API endpoint"
echo "4. Test launch with ./TestLauncher.exe"
