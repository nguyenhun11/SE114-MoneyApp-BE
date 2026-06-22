# Thông tin deploy và database
- Public API docs: [http://nguyenhun11-001-site1.site4future.com/swagger/index.html](http://nguyenhun11-001-site1.site4future.com/swagger/index.html)
- Thông tin kết nối Database: dùng SSMS hoặc các trình kết nối Database với các thông tin sau
  - **Server Name**: SQL1001.site4now.net
  - **Database Name**: db_ac9d5a_moneyapp
  - **Username**: db_ac9d5a_moneyapp_admin
  - **Password**: ********

# 💰 MoneyApp - Quản lý tài chính cá nhân (Back-end)

Dự án Backend cho ứng dụng quản lý thu chi cá nhân MoneyApp, được xây dựng trên nền tảng **ASP.NET Core 8 Web API** và **Entity Framework Core**.


## 🛠 1. Yêu cầu môi trường (Prerequisites)

Trước khi chạy dự án, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:

* **.NET 8.0 SDK** (Hoặc phiên bản mới hơn).
* **Visual Studio 2026** (Khuyên dùng - yêu cầu workload *ASP.NET and web development*)
* **SQL Server** (Có thể dùng LocalDB mặc định đi kèm Visual Studio, hoặc SQL Server Management Studio - SSMS).

---

## 📥 2. Các bước cài đặt (Installation Steps)

### Bước 1: Khởi mở chương trình

Clone về máy:

```bash
git clone https://github.com/nguyenhun11/SE114-MoneyApp-FE.git
```
Chạy file [SE114-MoneyApp-BE.slnx](./SE114-MoneyApp-BE.slnxSE114-MoneyApp-BE.slnx)

### Bước 2: Thiết lập cấu hình

1. Chạy lệnh để tạo `appsettings`
    ```
    cp ./SE114-MoneyApp-BE/appsettings.example.json ./SE114-MoneyApp-BE/appsettings.json
    cp ./SE114-MoneyApp-BE/appsettings.Development.example.json ./SE114-MoneyApp-BE/appsettings.Development.json
    ```

2. Đảm bảo nội dung `SE114-MoneyApp-BE/appsettings.json` có cấu trúc tương tự như sau:
    ```
    {
      "ConnectionStrings": {
        "DefaultConnection": "Data Source=SQL1001.site4now.net;Initial Catalog=DATABASE_NAME;User Id=DATABASE_USER_NAME;Password=DATABASE_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "GoogleAuth": {
        "ClientId": "GOOGLE_API_KEY.apps.googleusercontent.com"
      },
      "Jwt": {
        "Secret": "JWT_SECRET",
        "Issuer": "MoneyAppBackend",
        "Audience": "MoneyAppAndroid",
        "AccessTokenExpirationMinutes": 15,
        "RefreshTokenExpirationDays": 7
      },
      "SmtpSettings": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Username": "tranlekhanhhung2006@gmail.com",
        "Password": "SMTP_PASSWORD",
        "From": "MoneyApp <tranlekhanhhung2006@gmail.com>"
      },
      "ExchangeRateApi": {
        "BaseUrl": "https://v6.exchangerate-api.com/v6/",
        "ApiKey": "EXCHANGE_RATE_API_KEY",
        "BaseCurrency": "VND"
      },
      "AllowedHosts": "*"
    }
    ```
    Liên hệ các thành viên trong dự án để thay thế thông tin các biến:
    - `DATABASE_NAME`
    - `DATABASE_USER_NAME`
    - `DATABASE_PASSWORD`
    - `GOOGLE_API_KEY`
    - `JWT_SECRET`
    - `SMTP_PASSWORD`
    - `EXCHANGE_RATE_API_KEY`


### Bước 3: Phục hồi thư viện và Cập nhật Database

Bạn cần tải các thư viện Nuget và khởi tạo CSDL từ file Migrations có sẵn.

1. Mở menu **Tools** > **NuGet Package Manager** > **Package Manager Console**.
2. Gõ lệnh sau (Để tạo/cập nhật database trên máy local):
    ```powershell
    Update-Database
    ```
    Khi có thao tác thay đổi Models, cần chạy lệnh này để cập nhật db, sau đó mới update xuống local
    ```powershell
    Add-Migration <Tên thao tác>
    Update-Database
    ```

---

## ▶️ 3. Khởi chạy và Kiểm thử (Run & Test)

1. Nhấn **F5** (hoặc nút Run màu xanh lá) trên Visual Studio.
2. Trình duyệt sẽ tự động mở giao diện **Swagger UI** tại địa chỉ: `https://localhost:7011/swagger/index.html`.
3. Tải Postman và test thử các API thông thường
4. **Cách test API được bảo vệ:**
* Gọi API `POST /api/Auth/login` (hoặc register) để nhận được cặp chuỗi `Token` và `RefreshToken`.
* Copy chuỗi `Token`.
* Cuộn lên đầu trang Swagger, nhấn vào nút **Authorize** (Ổ khóa).
* Nhập theo cú pháp: `Bearer <dán_token_vào_đây>`.
* Nhấn **Authorize** và bắt đầu test các API khác (như Account, User).



---

## ⚠️ 4. Các lỗi thường gặp (Troubleshooting)

| Dấu hiệu lỗi | Nguyên nhân & Cách khắc phục |
| --- | --- |
| **A connection was successfully established with the server, but then an error occurred during the login process.** | Lỗi bảo mật SSL của .NET. Mở `appsettings.json`, kiểm tra xem chuỗi kết nối đã có đoạn `TrustServerCertificate=True;` ở cuối chưa. |
| **Port is already in use** (Ứng dụng văng ngay khi chạy) | Cổng cấu hình đang bị một dự án khác chiếm dụng. Mở file `Properties/launchSettings.json`, đổi số cổng (ví dụ: `7011` thành `7015`) ở trường `applicationUrl`. |
| **Mã lỗi 401 Unauthorized** khi gọi API có `[Authorize]` | Bạn chưa truyền Token vào Header, hoặc Token đã hết hạn (quá thời gian `AccessTokenExpirationMinutes`), hoặc thiếu cụm từ `Bearer` đứng trước Token. |
| **Lỗi 500 khi gọi Google Login** | Google Client ID trong `appsettings.json` bị sai hoặc không khớp với cấu hình Firebase Frontend của ứng dụng Android. |
