# Nattapong_7379_L002

## บัญชี Staff Demo

หน้าเข้าสู่ระบบรองรับทั้งชื่อผู้ใช้และอีเมล รหัสผ่านในตารางเป็นค่าตัวอย่างสำหรับบัญชีเดโมที่สร้างในเครื่องตามขั้นตอนด้านล่าง

| Role | ชื่อผู้ใช้ | อีเมล | รหัสผ่านตัวอย่าง |
| --- | --- | --- | --- |
| Admin | `admin` | `admin@gmail.com` | `LocalStaffDemo2026!` |
| Staff queue | `staffqueue` | `staffqueue@gmail.com` | `LocalStaffDemo2026!` |
| Kitchen staff | `staffkitchen` | `staffkitchen@gmail.com` | `LocalStaffDemo2026!` |
| Cashiers and staff | `staffcashier` | `staffcashier@gmail.com` | `LocalStaffDemo2026!` |
| Accounting | `staffaccount` | `staffaccount@gmail.com` | `LocalStaffDemo2026!` |
| Owner | `owner` | `owner@gmail.com` | `LocalStaffDemo2026!` |

### เปิดใช้บัญชีเดโมในเครื่อง

เมื่อฐานข้อมูลสำหรับ Development พร้อมแล้ว ให้รันคำสั่งต่อไปนี้จากโฟลเดอร์โปรเจกต์ คำสั่ง `dotnet user-secrets` จะเก็บค่าที่ตั้งไว้บนเครื่อง และโปรไฟล์ `http` จะเปิดแอปในโหมด Development

```powershell
dotnet user-secrets set 'DemoAccounts:Enabled' 'true'
dotnet user-secrets set 'DemoAccounts:Password' 'LocalStaffDemo2026!'
dotnet run --launch-profile http
```

เปิดหน้า [เข้าสู่ระบบ](http://localhost:5129/Account/Login) แล้วเลือกบัญชีจากตาราง

แอปจะใช้ค่า `DemoAccounts:Password` ร่วมกันตอนสร้างบัญชีเดโมใหม่ ค่านี้ต้องยาว 12–100 ตัวอักษร ถ้าฐานข้อมูลมีชื่อผู้ใช้หรืออีเมลตรงกันอยู่แล้ว ให้ใช้รหัสผ่านเดิมของบัญชีนั้น
