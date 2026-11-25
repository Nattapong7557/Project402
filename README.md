# Nattapong_7379_L002

## ฐานข้อมูลและบัญชี Staff

ตั้งค่า `ConnectionStrings:DefaultConnection` ให้ชี้ไปยังฐาน MySQL บน MonsterASP ผ่านการตั้งค่าของเว็บ สำหรับการรันในเครื่องสามารถเก็บค่านี้ไว้ใน .NET User Secrets ได้

ถ้าตั้งค่าฐานข้อมูลแล้ว ให้รันเว็บจากโฟลเดอร์โปรเจกต์

```powershell
dotnet run --launch-profile http
```

เปิดหน้า [เข้าสู่ระบบ](http://localhost:5129/Account/Login) แล้วใช้ชื่อผู้ใช้หรืออีเมลและรหัสผ่านของบัญชีที่มีอยู่ในฐานข้อมูล สิทธิ์ Staff มาจากบทบาทที่ผูกกับบัญชีในตาราง `staff` และ `roles` ผู้ดูแลระบบที่มีบัญชี Admin อยู่แล้วสามารถจัดการบัญชี Staff จากหน้า Admin
