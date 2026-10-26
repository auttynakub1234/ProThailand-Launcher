# ProThailand Master Server - Cloudflare Worker

Edge Serverless Worker ที่ทำหน้าที่เป็น Master Authentication Server แทน `api.prothailand.net/api.php` บน Cloudflare Edge ทั่วโลก

---

## ⚡ คุณสมบัติ
- **เร็วและประหยัด**: ทำงานบน Cloudflare Edge มี Latency ต่ำมากทั่วโลก ไม่ต้องเช่า VPS
- **ถอดรหัสผ่าน Web Crypto API**: ถอดรหัส AES-256-CBC + PBKDF2-HMAC-SHA256 (100,000 iterations) ด้วย Native Web Crypto API ของ Cloudflare
- **รองรับ CORS**: สามารถเรียกใช้งานได้จากทั้ง Client Application (`ProThailand.exe`) และ Web Browser
- **รองรับ Cloudflare KV**: เก็บข้อมูลผู้ใช้และผูก HWID ลงใน Cloudflare KV Storage ได้ทันที

---

## 🚀 วิธีการ Deploy ขึ้น Cloudflare Workers (2 วิธี)

### วิธีที่ 1: ผ่าน Cloudflare Dashboard (ง่ายที่สุด ไม่ต้องใช้ CLI)
1. เข้าสู่ระบบ [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. ไปที่เมนู **Workers & Pages** -> กดปุ่ม **Create application** -> เลือก **Create Worker**
3. ตั้งชื่อ Worker เช่น `prothailand-master-api` แล้วกด **Deploy**
4. กด **Edit code**
5. คัดลอกโค้ดทั้งหมดจากไฟล์ `worker.js` ไปวางทับโค้ดเดิมในหน้าต่าง Editor
6. กดปุ่ม **Save and deploy**
7. (ตั้งค่าโดเมน) ไปที่แท็บ **Settings** -> **Triggers** -> **Custom Domains** -> เพิ่มโดเมน `api.prothailand.net`

### วิธีที่ 2: ผ่าน Wrangler CLI
```bash
cd cloudflare-worker
npx wrangler login
npx wrangler deploy
```

---

## 🧪 การทดสอบ Endpoint
เมื่อ Deploy เสร็จแล้ว สามารถทดสอบเปิด URL ของ Worker:
```bash
curl https://prothailand-master-api.<your-subdomain>.workers.dev/api.php
```
จะได้รับ JSON:
```json
{
  "Server": "ProThailand Master API (Cloudflare Worker)",
  "Status": "ONLINE",
  "Version": "1.0.0",
  "Endpoint": "https://api.prothailand.net/api.php"
}
```
เมื่อ `ProThailand.exe` ส่งค่าล็อกอินและ HWID เข้ามา ระบบจะถอดรหัสและส่งผลลัพธ์ `LOGINOK` กลับไปให้ตัวเกมทันที
