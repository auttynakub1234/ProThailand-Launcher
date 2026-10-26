# ProThailand Suite (Live for Speed 0.6V Launcher & Master Server)

โปรเจกต์ Launcher และระบบยืนยันตัวตน Master Server สำหรับ Live for Speed (LFS 0.6V)
ถูกสร้างขึ้นใหม่ทั้งหมดเพื่อทดแทนไฟล์เดิมที่มีขนาดใหญ่ (143 MB) ให้เหลือเพียง **23 KB**

---

## 📁 โครงสร้างโปรเจกต์

```text
ProThailand/
├── ProThailand.exe              # ตัวโปรแกรม Launcher (Windows Executable 23 KB)
├── SecureCrypto.cs              # ตัวถอดรหัส/เข้ารหัส AES-256-CBC + PBKDF2 (SHA256, 100k iters)
├── HardwareInfo.cs              # ดึง HWID (MAC Address, Motherboard, BIOS UUID, HDD Serial)
├── LoginHandle.cs               # จัดการ Authentication ส่ง Payload ไปยัง Master Server
├── LFSManager.cs                # ตรวจจับ ค้นหา และเปิดเกม LFS.exe
├── FormMain.cs                  # หน้าต่าง GUI (Windows Forms) ธีม Dark Mode สวยงาม
├── Program.cs                   # Entry point รองรับทั้งโหมด GUI และ Command-line (--login, --inspect-hwid)
├── server_prothailand.py        # Master Server จำลองสำหรับรันบน Localhost (http://localhost:8080/api.php)
├── api.php                      # Endpoint ฝั่ง PHP พร้อมอัปโหลดขึ้นเว็บโฮสติ้ง (https://api.prothailand.net/api.php)
└── cloudflare-worker/           # Cloudflare Worker Serverless Endpoint
    ├── worker.js                # โค้ด Worker ใช้ Web Crypto API ถอดรหัส AES-256 บน Edge
    ├── wrangler.toml            # คอนฟิกการ Deploy Wrangler
    └── README.md                # คู่มือการ Deploy บน Cloudflare Dashboard & CLI
```

---

## 🚀 การใช้งาน `ProThailand.exe`

### 1. โหมด GUI (Windows)
ดับเบิ้ลคลิกไฟล์ `ProThailand.exe` เพื่อเปิดหน้าต่างกราฟิก:
- เลือก Server:
  - **Localhost Test Server** (`http://localhost:8080/api.php`)
  - **Production Server** (`https://api.prothailand.net/api.php`)
- กดปุ่ม **"🔍 Inspect HWID Fingerprint"** เพื่อดูข้อมูล Hardware ที่จะส่ง
- กรอก Username / Password แล้วกด **"🚀 Login & Authenticate"**
- เมื่อล็อกอินสำเร็จ กดปุ่ม **"🏁 Launch LFS.exe"** เพื่อเข้าเล่นเกม

### 2. โหมด Command-Line (CLI / Linux)
```bash
# ตรวจสอบค่า HWID ที่ตัวโปรแกรมอ่านได้จากเครื่อง
mono ProThailand.exe --inspect-hwid

# ทดสอบล็อกอินเข้า Localhost Server
mono ProThailand.exe --login Pilot01 123456 http://localhost:8080/api.php
```

---

## ☁️ การขึ้นระบบ Cloudflare Workers
ดูคู่มือการตั้งค่าแบบละเอียดในโฟลเดอร์ [`cloudflare-worker/README.md`](cloudflare-worker/README.md)
คัดลอกโค้ดจาก `cloudflare-worker/worker.js` ไปวางใน Cloudflare Workers Dashboard แล้วผูก Custom Domain `api.prothailand.net` ได้ทันที!
