# ProThailand LFS Authentication System

![ProThailand Banner](https://img.shields.io/badge/ProThailand-LFS%20Authentication-blue)
![Cloudflare Worker](https://img.shields.io/badge/Cloudflare-Worker-orange)
![C# Client](https://img.shields.io/badge/Client-C%23-green)

ระบบ Authentication สำหรับ Live for Speed (LFS) ที่ให้ผู้ใช้ล็อกอินผ่านเว็บและสามารถเล่นเกมด้วย account เดียวกัน

## 🎯 Features

### 🔐 Web Authentication
- สมัครสมาชิกผ่าน Web Portal
- ตรวจสอบผู้ใช้แบบ Real-time
- เก็บข้อมูลใน Cloudflare KV Storage

### 🎮 LFS Integration
- LocalMasterBridge (LFS Protocol Emulator)
- Memory Injection สำหรับ LFS.exe
- HWID Binding สำหรับความปลอดภัย

### 🌐 Cloudflare Edge
- ตอบกลับเร็วทั่วโลก
- Serverless Architecture
- Automatic Scaling

## 📁 Project Structure

```
prothailand-system/
├── client/                 # Windows Client (C#)
│   ├── ProThailand.exe    # Main executable
│   ├── *.cs              # Source code
│   └── README.txt        # Client guide
├── worker/                # Cloudflare Worker
│   ├── cloudflare-worker-full.js
│   └── wrangler-full.toml
├── servers/              # Local test servers
│   ├── *.py             # Python servers
│   └── api.php          # PHP endpoint
└── docs/                # Documentation
    └── DEPLOYMENT-GUIDE.md
```

## 🚀 Quick Start

### 1. Deploy Cloudflare Worker
```bash
# Install Wrangler CLI
npm install -g wrangler

# Login
wrangler login

# Create KV namespace
wrangler kv:namespace create "USERS_KV"

# Deploy
wrangler deploy --config worker/wrangler-full.toml
```

### 2. Setup Client
1. Edit hosts file: `127.0.0.1 m.lfspro.net`
2. Register user at: `https://your-worker.workers.dev/portal`
3. Run `client/ProThailand-new.exe`
4. Login → Start Bridge → Launch LFS

### 3. Use System
1. User registers on web portal
2. User logs in via ProThailand.exe
3. LFS connects to local bridge
4. Bridge verifies with Cloudflare API
5. User can play LFS with web account

## 🔧 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api.php` | GET | Main authentication endpoint |
| `/api/register` | POST | User registration |
| `/api/user` | GET | User lookup (for bridge) |
| `/portal` | GET | Web registration portal |

## 📊 How It Works

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│   Web User  │────▶│ Cloudflare   │────▶│   Database   │
│   Portal    │     │   Worker     │     │   (KV)       │
└─────────────┘     └──────────────┘     └──────────────┘
                           │
                           ▼
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│ ProThailand │────▶│ Local Master │────▶│   LFS.exe   │
│   Client    │     │   Bridge     │     │             │
└─────────────┘     └──────────────┘     └─────────────┘
```

## 🛠️ Configuration

### Cloudflare Worker
- Edit `worker/wrangler-full.toml`
- Set your Account ID and KV namespace
- Deploy with Wrangler CLI

### Client Application
- Edit hosts file for DNS redirect
- Configure server URL in GUI
- Use same credentials as web portal

## 📞 Support

- **Web Portal**: `https://your-worker.workers.dev/portal`
- **API Docs**: See `docs/DEPLOYMENT-GUIDE.md`
- **Client Guide**: See `client/README.txt`

## 📄 License

MIT License - See LICENSE file for details

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Commit changes
4. Push to branch
5. Create Pull Request

## ⭐ Star History

If you find this project useful, please give it a star! ⭐
