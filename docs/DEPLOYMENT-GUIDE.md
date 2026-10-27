# ProThailand Full System Deployment Guide
# Cloudflare Worker + Web Portal + LFS Authentication

## 🎯 ระบบที่ได้หลัง deployment:
1. **Web Portal** – `/portal` สำหรับสมัครสมาชิก
2. **API Endpoint** – `/api.php` สำหรับ ProThailand.exe
3. **User Database** – Cloudflare KV Storage
4. **LFS Bridge API** – `/api/user` สำหรับ LocalMasterBridge

## 🚀 ขั้นตอน Deploy:

### วิธีที่ 1: Cloudflare Dashboard (ง่าย)
1. เข้า [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Workers & Pages → Create application → Create Worker
3. ตั้งชื่อ `prothailand-master-api`
4. คัดลอกโค้ดจาก `cloudflare-worker-full.js` ไปวาง
5. Save and Deploy

### วิธีที่ 2: Wrangler CLI (มืออาชีพ)
```bash
# 1. Install Wrangler
npm install -g wrangler

# 2. Login
wrangler login

# 3. Create KV namespace
wrangler kv:namespace create "USERS_KV"
# เก็บ ID ที่ได้

# 4. แก้ไข wrangler-full.toml
# - YOUR_ACCOUNT_ID: จาก Dashboard
# - YOUR_KV_NAMESPACE_ID: จากขั้นตอนที่ 3
# - YOUR_ZONE_ID: จาก Cloudflare DNS

# 5. Deploy
wrangler deploy --config wrangler-full.toml
```

## 🔧 API Endpoints หลัง deploy:

### 1. Main API (สำหรับ ProThailand.exe)
```
GET https://your-worker.workers.dev/api.php
POST https://your-worker.workers.dev/api.php?data=ENCRYPTED_PAYLOAD
```

### 2. User Registration
```
POST https://your-worker.workers.dev/api/register
Content-Type: application/json
{
  "username": "Pilot01",
  "password": "123456",
  "email": "test@example.com"
}
```

### 3. User Lookup (สำหรับ LocalMasterBridge)
```
GET https://your-worker.workers.dev/api/user?name=Pilot01
```

### 4. Web Portal
```
https://your-worker.workers.dev/portal
```

## ⚙️ Configuration:

### 1. Cloudflare KV (สำหรับเก็บข้อมูลผู้ใช้)
- สร้าง namespace "USERS_KV"
- Binding ใน worker: `USERS_KV`

### 2. Custom Domain (optional)
ใน Worker Dashboard → Triggers → Custom Domains
เพิ่ม: `api.prothailand.net` หรือ domain ของคุณ

### 3. Environment Variables
ใน Dashboard → Settings → Variables
เพิ่ม: `MASTER_PASSWORD = "config.sys.input=UTF8"`

## 🔄 การอัปเดตโค้ด:
1. แก้ไข `cloudflare-worker-full.js`
2. ใน Dashboard → Edit code → Save and Deploy
หรือ
```bash
wrangler deploy --config wrangler-full.toml
```

## 📞 Testing:
```bash
# Test registration
curl -X POST https://your-worker.workers.dev/api/register \
  -H "Content-Type: application/json" \
  -d '{"username":"TestUser","password":"123456"}'

# Test user lookup
curl https://your-worker.workers.dev/api/user?name=TestUser

# Test main API
curl https://your-worker.workers.dev/api.php
```

## 🛠️ Troubleshooting:
1. **KV not found** – ต้องสร้าง KV namespace ก่อน
2. **CORS errors** – Worker มี CORS headers อยู่แล้ว
3. **Decryption failed** – ตรวจสอบ MASTER_PASSWORD ตรงกัน
4. **User not found** – สมัครสมาชิกก่อนใน /portal

## 🔗 Integrations:
- **ProThailand.exe** → `/api.php`
- **LocalMasterBridge** → `/api/user`
- **Web Browser** → `/portal`

