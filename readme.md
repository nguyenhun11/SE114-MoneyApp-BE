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

Dự án yêu cầu các cấu hình nhạy cảm (Chuỗi kết nối DB, Khóa bí mật JWT, Google Client ID) để hoạt động.

1. Tìm file `appsettings.json` trong thư mục gốc của Backend.
2. Đảm bảo nội dung file có cấu trúc tương tự như sau. Khuyến nghị sử dụng cấu trúc `LocalDB` mặc định dưới đây để không bị lỗi bảo mật chứng chỉ `.NET 8`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MoneyAppDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "GoogleAuth": {
    "ClientId": "YOUR_GOOGLE_WEB_CLIENT_ID.apps.googleusercontent.com"
  },
  "Jwt": {
    "Secret": "Gia_Hung_Khanh_Hung_Quang_Huy_Hoang_Son",
    "Issuer": "MoneyAppBackend",
    "Audience": "MoneyAppAndroid",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "AllowedHosts": "*"
}

```

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