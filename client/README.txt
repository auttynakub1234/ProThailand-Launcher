# ProThailand LFS Web Authentication System
# เวอร์ชันแก้ไขใหม่ - Web Login to LFS Game

## 🎯 ระบบทำงาน
User ล็อกอินผ่านเว็บ ProThailand → ข้อมูล sync ไปเกม LFS

## 📁 ไฟล์ใน pack นี้:
1. **ProThailand-new.exe** - โปรแกรมหลัก (compile ใหม่)
2. **FormMain.cs** - GUI ที่รวม LocalMasterBridge
3. **LocalMasterBridge.cs** - LFS Master Server Emulator
4. **LFSInjector.cs** - Inject credentials ลง memory LFS
5. ***.cs อื่นๆ** - ทั้งหมด 8 ไฟล์

## 🚀 ขั้นตอนการใช้งาน:

### 1. ตั้งค่า Hosts File
เปิด Notepad เป็น Administrator:
C:\Windows\System32\drivers\etc\hosts
เพิ่มบรรทัด:
```
127.0.0.1 m.lfspro.net
```

### 2. สมัครสมาชิกในเว็บ
เข้า: https://prothailand-master-api.fixflxw24.workers.dev
สมัคร user (เช่น Pilot01 / 123456)

### 3. ใช้งานโปรแกรม
1. เปิด `ProThailand-new.exe`
2. เลือก **Cloudflare Worker**
3. ใส่ Username/Password (จากเว็บ)
4. กด **"🚀 Login & Start Bridge"**
5. ถ้าสำเร็จ → กด **"🏁 Launch LFS.exe"**

### 4. ตรวจสอบในเกม
- LFS จะแสดง username จากเว็บ
- เชื่อมต่อ master server สำเร็จ

## 🔧 Technical Details:
- Web API: https://prothailand-master-api.fixflxw24.workers.dev
- Local Bridge: 127.0.0.1:29339
- Memory Injection: Username/Password + Master Server

## ⚠️ ข้อควรรู้:
- ต้องมี LFS.exe ในเครื่อง
- ต้องรันโปรแกรมเป็น Administrator
- Hosts file ต้องแก้ไขให้ถูกต้อง

## 📞 Support:
เว็บ: https://prothailand-master-api.fixflxw24.workers.dev
