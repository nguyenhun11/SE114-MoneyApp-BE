using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(AppDbContext context)
        {
            // Nếu DB đã có dữ liệu thì bỏ qua
            if (await context.Users.AnyAsync()) return;

            var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // ================================================================
            // 1. TẠO NGƯỜI DÙNG
            // ================================================================

            // User 1: Người dùng chính – nhiều account, category, giao dịch
            var user1 = new User
            {
                Name = "Nguyễn Văn An",
                Email = "an.nguyen@gmail.com",
                PhoneNumber = "0901234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                DailyStreak = 30,
                LastActiveDate = DateTime.UtcNow,
                CreatedAt = baseDate,
                LastUpdatedAt = DateTime.UtcNow
            };

            // User 2: Người dùng thứ hai – vài account, ít giao dịch hơn
            var user2 = new User
            {
                Name = "Trần Thị Bình",
                Email = "binh.tran@gmail.com",
                PhoneNumber = "0912345678",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                DailyStreak = 7,
                LastActiveDate = DateTime.UtcNow,
                CreatedAt = baseDate.AddDays(15),
                LastUpdatedAt = DateTime.UtcNow
            };

            // User 3: Người dùng thứ ba – mới tạo, ít dữ liệu
            var user3 = new User
            {
                Name = "Lê Minh Cường",
                Email = "cuong.le@gmail.com",
                PhoneNumber = "0987654321",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                DailyStreak = 1,
                LastActiveDate = DateTime.UtcNow,
                CreatedAt = baseDate.AddMonths(4),
                LastUpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(user1, user2, user3);

            // ================================================================
            // 2. TẠO TÀI KHOẢN (USER 1 – nhiều account)
            // ================================================================

            var u1Cash = new Account
            {
                User = user1,
                AccountName = "Tiền mặt",
                Balance = 2_350_000,
                ColorId = 1,
                IconId = 1,
                IncludeInTotalBalance = true,
                SortingOrder = 0
            };
            var u1TPBank = new Account
            {
                User = user1,
                AccountName = "Thẻ TPBank",
                Balance = 18_500_000,
                ColorId = 2,
                IconId = 2,
                IncludeInTotalBalance = true,
                SortingOrder = 1
            };
            var u1VCB = new Account
            {
                User = user1,
                AccountName = "Vietcombank",
                Balance = 45_000_000,
                ColorId = 3,
                IconId = 3,
                IncludeInTotalBalance = true,
                SortingOrder = 2
            };
            var u1MBBank = new Account
            {
                User = user1,
                AccountName = "MBBank",
                Balance = 5_200_000,
                ColorId = 4,
                IconId = 4,
                IncludeInTotalBalance = true,
                SortingOrder = 3
            };
            var u1Savings = new Account
            {
                User = user1,
                AccountName = "Tiết kiệm VCB",
                Balance = 100_000_000,
                ColorId = 5,
                IconId = 5,
                IncludeInTotalBalance = false,   // không tính vào tổng
                SortingOrder = 4
            };
            var u1MoMo = new Account
            {
                User = user1,
                AccountName = "Ví MoMo",
                Balance = 750_000,
                ColorId = 6,
                IconId = 6,
                IncludeInTotalBalance = true,
                SortingOrder = 5
            };
            context.Accounts.AddRange(u1Cash, u1TPBank, u1VCB, u1MBBank, u1Savings, u1MoMo);

            // Tài khoản User 2
            var u2Cash = new Account
            {
                User = user2,
                AccountName = "Tiền mặt",
                Balance = 800_000,
                ColorId = 1,
                IconId = 1,
                IncludeInTotalBalance = true,
                SortingOrder = 0
            };
            var u2Techcombank = new Account
            {
                User = user2,
                AccountName = "Techcombank",
                Balance = 22_000_000,
                ColorId = 2,
                IconId = 2,
                IncludeInTotalBalance = true,
                SortingOrder = 1
            };
            var u2ZaloPay = new Account
            {
                User = user2,
                AccountName = "ZaloPay",
                Balance = 300_000,
                ColorId = 7,
                IconId = 7,
                IncludeInTotalBalance = true,
                SortingOrder = 2
            };
            context.Accounts.AddRange(u2Cash, u2Techcombank, u2ZaloPay);

            // Tài khoản User 3
            var u3Cash = new Account
            {
                User = user3,
                AccountName = "Tiền mặt",
                Balance = 500_000,
                ColorId = 1,
                IconId = 1,
                IncludeInTotalBalance = true,
                SortingOrder = 0
            };
            var u3VietinBank = new Account
            {
                User = user3,
                AccountName = "VietinBank",
                Balance = 3_000_000,
                ColorId = 2,
                IconId = 2,
                IncludeInTotalBalance = true,
                SortingOrder = 1
            };
            context.Accounts.AddRange(u3Cash, u3VietinBank);

            // ================================================================
            // 3. TẠO NHÓM HẠNG MỤC VÀ DANH MỤC (USER 1 – nhiều category)
            // ================================================================

            // --- TẠO NHÓM HẠNG MỤC (GROUP) CHO USER 1 ---
            // Nhóm Chi tiêu
            var ug1Living = new CategoryGroup { User = user1, GroupName = "Sinh hoạt", Type = CategoryType.Expense, SortingOrder = 0 };
            var ug1Personal = new CategoryGroup { User = user1, GroupName = "Cá nhân", Type = CategoryType.Expense, SortingOrder = 1 };
            var ug1EduEnt = new CategoryGroup { User = user1, GroupName = "Phát triển & Giải trí", Type = CategoryType.Expense, SortingOrder = 2 };
            var ug1ExpOther = new CategoryGroup { User = user1, GroupName = "Khác", Type = CategoryType.Expense, SortingOrder = 3 };

            // Nhóm Thu nhập
            var ug1IncomeMain = new CategoryGroup { User = user1, GroupName = "Thu nhập chính", Type = CategoryType.Income, SortingOrder = 0 };
            var ug1Invest = new CategoryGroup { User = user1, GroupName = "Đầu tư", Type = CategoryType.Income, SortingOrder = 1 };
            var ug1IncOther = new CategoryGroup { User = user1, GroupName = "Khác", Type = CategoryType.Income, SortingOrder = 2 };

            context.CategoryGroups.AddRange(ug1Living, ug1Personal, ug1EduEnt, ug1ExpOther, ug1IncomeMain, ug1Invest, ug1IncOther);


            // --- TẠO DANH MỤC (CATEGORY) CHO USER 1 ---
            // --- Chi tiêu ---
            var c1Food = new Category { User = user1, CategoryGroup = ug1Living, CategoryName = "Ăn uống", ColorId = 1, IconId = 1, SortingOrder = 0 };
            var c1Rent = new Category { User = user1, CategoryGroup = ug1Living, CategoryName = "Thuê nhà", ColorId = 7, IconId = 7, SortingOrder = 1 };
            var c1Utilities = new Category { User = user1, CategoryGroup = ug1Living, CategoryName = "Điện nước", ColorId = 8, IconId = 8, SortingOrder = 2 };
            var c1Transport = new Category { User = user1, CategoryGroup = ug1Living, CategoryName = "Đi lại", ColorId = 2, IconId = 2, SortingOrder = 3 };

            var c1Shopping = new Category { User = user1, CategoryGroup = ug1Personal, CategoryName = "Mua sắm", ColorId = 3, IconId = 3, SortingOrder = 0 };
            var c1Health = new Category { User = user1, CategoryGroup = ug1Personal, CategoryName = "Sức khỏe", ColorId = 4, IconId = 4, SortingOrder = 1 };
            var c1Personal = new Category { User = user1, CategoryGroup = ug1Personal, CategoryName = "Cá nhân", ColorId = 9, IconId = 9, SortingOrder = 2 };
            var c1Gift = new Category { User = user1, CategoryGroup = ug1Personal, CategoryName = "Quà tặng", ColorId = 10, IconId = 10, SortingOrder = 3 };

            var c1Education = new Category { User = user1, CategoryGroup = ug1EduEnt, CategoryName = "Học tập", ColorId = 5, IconId = 5, SortingOrder = 0 };
            var c1Entertainment = new Category { User = user1, CategoryGroup = ug1EduEnt, CategoryName = "Giải trí", ColorId = 6, IconId = 6, SortingOrder = 1 };

            var c1DefaultExpense = new Category { User = user1, CategoryGroup = ug1ExpOther, CategoryName = "Khác", ColorId = 11, IconId = 11, SortingOrder = 0 };

            // --- Thu nhập ---
            var c1Salary = new Category { User = user1, CategoryGroup = ug1IncomeMain, CategoryName = "Lương", ColorId = 11, IconId = 11, SortingOrder = 0 };
            var c1Freelance = new Category { User = user1, CategoryGroup = ug1IncomeMain, CategoryName = "Freelance", ColorId = 12, IconId = 12, SortingOrder = 1 };
            var c1Bonus = new Category { User = user1, CategoryGroup = ug1IncomeMain, CategoryName = "Thưởng", ColorId = 14, IconId = 14, SortingOrder = 2 };

            var c1Investment = new Category { User = user1, CategoryGroup = ug1Invest, CategoryName = "Đầu tư", ColorId = 13, IconId = 13, SortingOrder = 0 };

            var c1Other = new Category { User = user1, CategoryGroup = ug1IncOther, CategoryName = "Khác", ColorId = 15, IconId = 15, SortingOrder = 0 };
            var c1DefaultIncome = new Category { User = user1, CategoryGroup = ug1IncOther, CategoryName = "Khác", ColorId = 16, IconId = 16, SortingOrder = 1 };

            context.Categories.AddRange(
                c1Food, c1Transport, c1Shopping, c1Health, c1Education,
                c1Entertainment, c1Rent, c1Utilities, c1Personal, c1Gift,
                c1Salary, c1Freelance, c1Investment, c1Bonus, c1Other, c1DefaultExpense, c1DefaultIncome
            );


            // ================================================================
            // USER 2
            // ================================================================
            var ug2Daily = new CategoryGroup { User = user2, GroupName = "Hàng ngày", Type = CategoryType.Expense, SortingOrder = 0 };
            var ug2ExpOther = new CategoryGroup { User = user2, GroupName = "Khác", Type = CategoryType.Expense, SortingOrder = 1 };
            var ug2Income = new CategoryGroup { User = user2, GroupName = "Thu nhập", Type = CategoryType.Income, SortingOrder = 0 };

            context.CategoryGroups.AddRange(ug2Daily, ug2ExpOther, ug2Income);

            var c2Food = new Category { User = user2, CategoryGroup = ug2Daily, CategoryName = "Ăn uống", ColorId = 1, IconId = 1, SortingOrder = 0 };
            var c2Shopping = new Category { User = user2, CategoryGroup = ug2Daily, CategoryName = "Mua sắm", ColorId = 2, IconId = 2, SortingOrder = 1 };
            var c2Transport = new Category { User = user2, CategoryGroup = ug2Daily, CategoryName = "Di chuyển", ColorId = 3, IconId = 3, SortingOrder = 2 };
            var c2DefaultExpense = new Category { User = user2, CategoryGroup = ug2ExpOther, CategoryName = "Khác", ColorId = 6, IconId = 6, IsDefault = true, SortingOrder = 0 };

            var c2Salary = new Category { User = user2, CategoryGroup = ug2Income, CategoryName = "Lương", ColorId = 4, IconId = 4, SortingOrder = 0 };
            var c2SideJob = new Category { User = user2, CategoryGroup = ug2Income, CategoryName = "Việc phụ", ColorId = 5, IconId = 5, SortingOrder = 1 };
            var c2DefaultIncome = new Category { User = user2, CategoryGroup = ug2Income, CategoryName = "Khác", ColorId = 7, IconId = 7, IsDefault = true, SortingOrder = 2 };

            context.Categories.AddRange(c2Food, c2Shopping, c2Transport, c2Salary, c2SideJob, c2DefaultExpense, c2DefaultIncome);


            // ================================================================
            // USER 3
            // ================================================================
            var ug3Expense = new CategoryGroup { User = user3, GroupName = "Chi tiêu", Type = CategoryType.Expense, SortingOrder = 0 };
            var ug3Income = new CategoryGroup { User = user3, GroupName = "Thu nhập", Type = CategoryType.Income, SortingOrder = 0 };

            context.CategoryGroups.AddRange(ug3Expense, ug3Income);

            var c3Food = new Category { User = user3, CategoryGroup = ug3Expense, CategoryName = "Ăn uống", ColorId = 1, IconId = 1, SortingOrder = 0 };
            var c3DefaultExpense = new Category { User = user3, CategoryGroup = ug3Expense, CategoryName = "Khác", ColorId = 3, IconId = 3, IsDefault = true, SortingOrder = 1 };

            var c3Salary = new Category { User = user3, CategoryGroup = ug3Income, CategoryName = "Lương", ColorId = 2, IconId = 2, SortingOrder = 0 };
            var c3DefaultIncome = new Category { User = user3, CategoryGroup = ug3Income, CategoryName = "Khác", ColorId = 4, IconId = 4, IsDefault = true, SortingOrder = 1 };

            context.Categories.AddRange(c3Food, c3Salary, c3DefaultExpense, c3DefaultIncome);

            // ================================================================
            // 4. TẠO GIAO DỊCH – USER 1 (rải đều 6 tháng, Jan–Jun 2025)
            // ================================================================

            var transactions = new List<Transaction>();

            // ---- THÁNG 1 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 18_000_000, TransactionDate = D(2025,1,5),  Note = "Lương tháng 1" },
                new Transaction { Account = u1TPBank, Category = c1Freelance,     Amount = 3_500_000,  TransactionDate = D(2025,1,8),  Note = "Dự án web freelance" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 55_000,     TransactionDate = D(2025,1,3),  Note = "Ăn sáng phở" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 120_000,    TransactionDate = D(2025,1,5),  Note = "Cơm trưa văn phòng" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 85_000,     TransactionDate = D(2025,1,7),  Note = "Bún bò bữa tối" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,1,2),  Note = "Tiền thuê nhà T1" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 350_000,    TransactionDate = D(2025,1,10), Note = "Tiền điện tháng 1" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 90_000,     TransactionDate = D(2025,1,10), Note = "Tiền nước tháng 1" },
                new Transaction { Account = u1MoMo,   Category = c1Transport,     Amount = 45_000,     TransactionDate = D(2025,1,6),  Note = "Grab đi làm" },
                new Transaction { Account = u1MoMo,   Category = c1Transport,     Amount = 38_000,     TransactionDate = D(2025,1,9),  Note = "Grab về nhà" },
                new Transaction { Account = u1TPBank, Category = c1Transport,     Amount = 200_000,    TransactionDate = D(2025,1,15), Note = "Đổ xăng xe máy" },
                new Transaction { Account = u1TPBank, Category = c1Shopping,      Amount = 850_000,    TransactionDate = D(2025,1,18), Note = "Mua quần áo Tết" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 430_000,    TransactionDate = D(2025,1,20), Note = "Ăn tất niên với gia đình" },
                new Transaction { Account = u1VCB,    Category = c1Gift,          Amount = 1_200_000,  TransactionDate = D(2025,1,25), Note = "Mua quà Tết tặng bố mẹ" },
                new Transaction { Account = u1Cash,   Category = c1Entertainment, Amount = 180_000,    TransactionDate = D(2025,1,28), Note = "Xem phim Tết" },
                new Transaction { Account = u1VCB,    Category = c1Bonus,         Amount = 5_000_000,  TransactionDate = D(2025,1,30), Note = "Thưởng Tết công ty" },
            });

            // ---- THÁNG 2 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 18_000_000, TransactionDate = D(2025,2,5),  Note = "Lương tháng 2" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 65_000,     TransactionDate = D(2025,2,2),  Note = "Ăn sáng bánh mì" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 110_000,    TransactionDate = D(2025,2,5),  Note = "Cơm văn phòng" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 280_000,    TransactionDate = D(2025,2,14), Note = "Ăn tối Valentine" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,2,2),  Note = "Tiền thuê nhà T2" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 320_000,    TransactionDate = D(2025,2,10), Note = "Tiền điện tháng 2" },
                new Transaction { Account = u1MBBank, Category = c1Health,        Amount = 250_000,    TransactionDate = D(2025,2,8),  Note = "Khám sức khỏe định kỳ" },
                new Transaction { Account = u1MBBank, Category = c1Health,        Amount = 180_000,    TransactionDate = D(2025,2,9),  Note = "Mua thuốc" },
                new Transaction { Account = u1TPBank, Category = c1Education,     Amount = 1_200_000,  TransactionDate = D(2025,2,12), Note = "Học phí khóa tiếng Anh" },
                new Transaction { Account = u1MoMo,   Category = c1Transport,     Amount = 55_000,     TransactionDate = D(2025,2,15), Note = "Grab đi làm" },
                new Transaction { Account = u1VCB,    Category = c1Shopping,      Amount = 450_000,    TransactionDate = D(2025,2,20), Note = "Mua sách kỹ năng" },
                new Transaction { Account = u1TPBank, Category = c1Entertainment, Amount = 300_000,    TransactionDate = D(2025,2,22), Note = "Karaoke với đồng nghiệp" },
                new Transaction { Account = u1TPBank, Category = c1Personal,      Amount = 150_000,    TransactionDate = D(2025,2,25), Note = "Cắt tóc" },
                new Transaction { Account = u1VCB,    Category = c1Freelance,     Amount = 2_800_000,  TransactionDate = D(2025,2,28), Note = "Thiết kế UI freelance" },
            });

            // ---- THÁNG 3 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 18_000_000, TransactionDate = D(2025,3,5),  Note = "Lương tháng 3" },
                new Transaction { Account = u1VCB,    Category = c1Investment,    Amount = 1_500_000,  TransactionDate = D(2025,3,1),  Note = "Cổ tức chứng khoán" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 75_000,     TransactionDate = D(2025,3,4),  Note = "Ăn sáng + cà phê" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 95_000,     TransactionDate = D(2025,3,7),  Note = "Bữa trưa" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 350_000,    TransactionDate = D(2025,3,20), Note = "Tiệc sinh nhật bạn bè" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,3,2),  Note = "Tiền thuê nhà T3" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 410_000,    TransactionDate = D(2025,3,10), Note = "Tiền điện + nước T3" },
                new Transaction { Account = u1TPBank, Category = c1Transport,     Amount = 220_000,    TransactionDate = D(2025,3,14), Note = "Đổ xăng" },
                new Transaction { Account = u1VCB,    Category = c1Shopping,      Amount = 2_300_000,  TransactionDate = D(2025,3,16), Note = "Mua giày Nike" },
                new Transaction { Account = u1MBBank, Category = c1Education,     Amount = 1_200_000,  TransactionDate = D(2025,3,12), Note = "Học phí tiếng Anh T3" },
                new Transaction { Account = u1MBBank, Category = c1Health,        Amount = 350_000,    TransactionDate = D(2025,3,18), Note = "Mua vitamin tổng hợp" },
                new Transaction { Account = u1TPBank, Category = c1Entertainment, Amount = 250_000,    TransactionDate = D(2025,3,22), Note = "Xem hòa nhạc" },
                new Transaction { Account = u1MoMo,   Category = c1Food,          Amount = 180_000,    TransactionDate = D(2025,3,25), Note = "Order Grab Food" },
                new Transaction { Account = u1MoMo,   Category = c1Transport,     Amount = 62_000,     TransactionDate = D(2025,3,27), Note = "Grab đi khám bệnh" },
                new Transaction { Account = u1VCB,    Category = c1Gift,          Amount = 500_000,    TransactionDate = D(2025,3,8),  Note = "Quà 8/3 cho mẹ" },
            });

            // ---- THÁNG 4 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 18_000_000, TransactionDate = D(2025,4,5),  Note = "Lương tháng 4" },
                new Transaction { Account = u1VCB,    Category = c1Freelance,     Amount = 4_200_000,  TransactionDate = D(2025,4,10), Note = "Dự án app mobile" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 60_000,     TransactionDate = D(2025,4,3),  Note = "Ăn sáng" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 130_000,    TransactionDate = D(2025,4,8),  Note = "Cơm trưa" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 95_000,     TransactionDate = D(2025,4,15), Note = "Bún chả bữa tối" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 460_000,    TransactionDate = D(2025,4,25), Note = "Buffet lẩu cuối tuần" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,4,2),  Note = "Tiền thuê nhà T4" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 380_000,    TransactionDate = D(2025,4,10), Note = "Điện nước T4" },
                new Transaction { Account = u1TPBank, Category = c1Transport,     Amount = 215_000,    TransactionDate = D(2025,4,14), Note = "Đổ xăng" },
                new Transaction { Account = u1VCB,    Category = c1Shopping,      Amount = 1_800_000,  TransactionDate = D(2025,4,18), Note = "Mua tai nghe Sony" },
                new Transaction { Account = u1MBBank, Category = c1Education,     Amount = 1_200_000,  TransactionDate = D(2025,4,12), Note = "Học phí tiếng Anh T4" },
                new Transaction { Account = u1TPBank, Category = c1Entertainment, Amount = 150_000,    TransactionDate = D(2025,4,20), Note = "Phim cuối tuần" },
                new Transaction { Account = u1TPBank, Category = c1Personal,      Amount = 200_000,    TransactionDate = D(2025,4,22), Note = "Chăm sóc da mặt" },
                new Transaction { Account = u1MoMo,   Category = c1Food,          Amount = 220_000,    TransactionDate = D(2025,4,28), Note = "Order ShopeeFood" },
                new Transaction { Account = u1VCB,    Category = c1Investment,    Amount = 800_000,    TransactionDate = D(2025,4,30), Note = "Lãi tiết kiệm online" },
            });

            // ---- THÁNG 5 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 18_000_000, TransactionDate = D(2025,5,5),  Note = "Lương tháng 5" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 70_000,     TransactionDate = D(2025,5,2),  Note = "Ăn sáng hủ tiếu" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 115_000,    TransactionDate = D(2025,5,6),  Note = "Cơm bình dân" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 390_000,    TransactionDate = D(2025,5,11), Note = "Nhà hàng kỷ niệm" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,5,2),  Note = "Tiền thuê nhà T5" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 450_000,    TransactionDate = D(2025,5,10), Note = "Điện nước T5 (mùa hè)" },
                new Transaction { Account = u1TPBank, Category = c1Transport,     Amount = 240_000,    TransactionDate = D(2025,5,14), Note = "Đổ xăng" },
                new Transaction { Account = u1VCB,    Category = c1Shopping,      Amount = 3_200_000,  TransactionDate = D(2025,5,18), Note = "Mua bàn phím cơ" },
                new Transaction { Account = u1MBBank, Category = c1Education,     Amount = 1_200_000,  TransactionDate = D(2025,5,12), Note = "Học phí tiếng Anh T5" },
                new Transaction { Account = u1MBBank, Category = c1Health,        Amount = 600_000,    TransactionDate = D(2025,5,20), Note = "Khám nha sĩ" },
                new Transaction { Account = u1TPBank, Category = c1Entertainment, Amount = 450_000,    TransactionDate = D(2025,5,24), Note = "Du lịch Vũng Tàu" },
                new Transaction { Account = u1MoMo,   Category = c1Transport,     Amount = 75_000,     TransactionDate = D(2025,5,26), Note = "Grab sân bay" },
                new Transaction { Account = u1VCB,    Category = c1Freelance,     Amount = 5_500_000,  TransactionDate = D(2025,5,28), Note = "Dự án backend API" },
                new Transaction { Account = u1VCB,    Category = c1Gift,          Amount = 800_000,    TransactionDate = D(2025,5,30), Note = "Quà sinh nhật đồng nghiệp" },
            });

            // ---- THÁNG 6 / 2025 ----
            transactions.AddRange(new[]
            {
                new Transaction { Account = u1VCB,    Category = c1Salary,        Amount = 20_000_000, TransactionDate = D(2025,6,5),  Note = "Lương T6 (tăng lương)" },
                new Transaction { Account = u1VCB,    Category = c1Bonus,         Amount = 3_000_000,  TransactionDate = D(2025,6,5),  Note = "Thưởng KPI Q2" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 80_000,     TransactionDate = D(2025,6,3),  Note = "Ăn sáng" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 145_000,    TransactionDate = D(2025,6,8),  Note = "Cơm + trà sữa" },
                new Transaction { Account = u1Cash,   Category = c1Food,          Amount = 520_000,    TransactionDate = D(2025,6,15), Note = "Tiệc liên hoan tăng lương" },
                new Transaction { Account = u1TPBank, Category = c1Rent,          Amount = 4_500_000,  TransactionDate = D(2025,6,2),  Note = "Tiền thuê nhà T6" },
                new Transaction { Account = u1TPBank, Category = c1Utilities,     Amount = 520_000,    TransactionDate = D(2025,6,10), Note = "Điện nước T6" },
                new Transaction { Account = u1TPBank, Category = c1Transport,     Amount = 230_000,    TransactionDate = D(2025,6,13), Note = "Đổ xăng" },
                new Transaction { Account = u1VCB,    Category = c1Shopping,      Amount = 5_500_000,  TransactionDate = D(2025,6,20), Note = "Mua điện thoại Samsung A55" },
                new Transaction { Account = u1MBBank, Category = c1Education,     Amount = 2_500_000,  TransactionDate = D(2025,6,12), Note = "Đăng ký khóa học Udemy" },
                new Transaction { Account = u1TPBank, Category = c1Entertainment, Amount = 800_000,    TransactionDate = D(2025,6,22), Note = "Đi Đà Lạt 1 ngày" },
                new Transaction { Account = u1MoMo,   Category = c1Food,          Amount = 195_000,    TransactionDate = D(2025,6,25), Note = "Order Baemin" },
                new Transaction { Account = u1VCB,    Category = c1Investment,    Amount = 2_000_000,  TransactionDate = D(2025,6,28), Note = "Lợi nhuận chứng khoán tháng 6" },
                new Transaction { Account = u1TPBank, Category = c1Personal,      Amount = 350_000,    TransactionDate = D(2025,6,28), Note = "Mua nước hoa" },
                new Transaction { Account = u1VCB,    Category = c1Other,         Amount = 1_000_000,  TransactionDate = D(2025,6,30), Note = "Hoàn tiền bảo hiểm" },
            });

            // ================================================================
            // 5. GIAO DỊCH USER 2 (Tháng 3–6/2025)
            // ================================================================
            transactions.AddRange(new[]
            {
                new Transaction { Account = u2Techcombank, Category = c2Salary,   Amount = 12_000_000, TransactionDate = D(2025,3,3),  Note = "Lương tháng 3" },
                new Transaction { Account = u2Cash,        Category = c2Food,     Amount = 80_000,     TransactionDate = D(2025,3,5),  Note = "Ăn sáng + cà phê" },
                new Transaction { Account = u2Cash,        Category = c2Food,     Amount = 150_000,    TransactionDate = D(2025,3,10), Note = "Cơm trưa + tráng miệng" },
                new Transaction { Account = u2Techcombank, Category = c2Shopping, Amount = 1_500_000,  TransactionDate = D(2025,3,15), Note = "Mua đồ gia dụng" },
                new Transaction { Account = u2ZaloPay,     Category = c2Transport,Amount = 35_000,     TransactionDate = D(2025,3,18), Note = "Grab di chuyển" },

                new Transaction { Account = u2Techcombank, Category = c2Salary,   Amount = 12_000_000, TransactionDate = D(2025,4,3),  Note = "Lương tháng 4" },
                new Transaction { Account = u2Techcombank, Category = c2SideJob,  Amount = 2_500_000,  TransactionDate = D(2025,4,20), Note = "Dạy gia sư" },
                new Transaction { Account = u2Cash,        Category = c2Food,     Amount = 95_000,     TransactionDate = D(2025,4,6),  Note = "Bữa trưa" },
                new Transaction { Account = u2Techcombank, Category = c2Shopping, Amount = 800_000,    TransactionDate = D(2025,4,25), Note = "Mua mỹ phẩm" },
                new Transaction { Account = u2ZaloPay,     Category = c2Transport,Amount = 48_000,     TransactionDate = D(2025,4,28), Note = "Grab về nhà" },

                new Transaction { Account = u2Techcombank, Category = c2Salary,   Amount = 12_000_000, TransactionDate = D(2025,5,3),  Note = "Lương tháng 5" },
                new Transaction { Account = u2Cash,        Category = c2Food,     Amount = 120_000,    TransactionDate = D(2025,5,7),  Note = "Ăn uống" },
                new Transaction { Account = u2Techcombank, Category = c2Shopping, Amount = 2_200_000,  TransactionDate = D(2025,5,15), Note = "Mua đồ thể thao" },

                new Transaction { Account = u2Techcombank, Category = c2Salary,   Amount = 12_000_000, TransactionDate = D(2025,6,3),  Note = "Lương tháng 6" },
                new Transaction { Account = u2Techcombank, Category = c2SideJob,  Amount = 3_000_000,  TransactionDate = D(2025,6,18), Note = "Gia sư tháng 6" },
                new Transaction { Account = u2Cash,        Category = c2Food,     Amount = 200_000,    TransactionDate = D(2025,6,20), Note = "Tiệc cuối tháng" },
            });

            // ================================================================
            // 6. GIAO DỊCH USER 3 (Tháng 5–6/2025 – mới dùng)
            // ================================================================
            transactions.AddRange(new[]
            {
                new Transaction { Account = u3VietinBank, Category = c3Salary, Amount = 8_000_000, TransactionDate = D(2025,5,5),  Note = "Lương đầu tháng 5" },
                new Transaction { Account = u3Cash,       Category = c3Food,   Amount = 50_000,    TransactionDate = D(2025,5,6),  Note = "Ăn sáng" },
                new Transaction { Account = u3Cash,       Category = c3Food,   Amount = 90_000,    TransactionDate = D(2025,5,12), Note = "Bữa trưa" },
                new Transaction { Account = u3VietinBank, Category = c3Salary, Amount = 8_000_000, TransactionDate = D(2025,6,5),  Note = "Lương tháng 6" },
                new Transaction { Account = u3Cash,       Category = c3Food,   Amount = 75_000,    TransactionDate = D(2025,6,8),  Note = "Ăn sáng + nước" },
            });

            context.Transactions.AddRange(transactions);

            // ================================================================
            // 7. CHUYỂN KHOẢN – TRANSFERS (User 1 – nhiều lần)
            // ================================================================
            var transfers = new List<Transfer>
            {
                // T1: Rút tiền mặt từ TPBank
                new Transfer { Source = u1TPBank, Destination = u1Cash,    Amount = 2_000_000, TransferDate = D(2025,1,6),  Description = "Rút tiền mặt tiêu Tết" },
                // T1: Nạp MoMo từ VCB
                new Transfer { Source = u1VCB,    Destination = u1MoMo,    Amount = 500_000,   TransferDate = D(2025,1,10), Description = "Nạp ví MoMo" },

                // T2: Rút tiền mặt
                new Transfer { Source = u1VCB,    Destination = u1Cash,    Amount = 1_000_000, TransferDate = D(2025,2,3),  Description = "Rút tiền mặt tiêu vặt" },
                // T2: Nạp MoMo
                new Transfer { Source = u1TPBank, Destination = u1MoMo,    Amount = 300_000,   TransferDate = D(2025,2,20), Description = "Nạp MoMo đặt đồ ăn" },

                // T3: Gửi tiết kiệm
                new Transfer { Source = u1VCB,    Destination = u1Savings, Amount = 5_000_000, TransferDate = D(2025,3,6),  Description = "Gửi tiết kiệm tháng 3" },
                // T3: Rút tiền mặt
                new Transfer { Source = u1TPBank, Destination = u1Cash,    Amount = 800_000,   TransferDate = D(2025,3,15), Description = "Rút tiền mặt" },

                // T4: Rút MBBank về tiền mặt
                new Transfer { Source = u1MBBank, Destination = u1Cash,    Amount = 500_000,   TransferDate = D(2025,4,5),  Description = "Rút MBBank tiêu vặt" },
                // T4: Nạp MoMo từ TPBank
                new Transfer { Source = u1TPBank, Destination = u1MoMo,    Amount = 400_000,   TransferDate = D(2025,4,22), Description = "Nạp MoMo order đồ ăn" },
                // T4: Gửi tiết kiệm
                new Transfer { Source = u1VCB,    Destination = u1Savings, Amount = 5_000_000, TransferDate = D(2025,4,30), Description = "Gửi tiết kiệm tháng 4" },

                // T5: Rút tiền
                new Transfer { Source = u1VCB,    Destination = u1Cash,    Amount = 1_500_000, TransferDate = D(2025,5,3),  Description = "Rút tiền mặt tháng 5" },
                // T5: Gửi tiết kiệm
                new Transfer { Source = u1VCB,    Destination = u1Savings, Amount = 5_000_000, TransferDate = D(2025,5,31), Description = "Gửi tiết kiệm tháng 5" },

                // T6: Gửi tiết kiệm lớn sau tăng lương
                new Transfer { Source = u1VCB,    Destination = u1Savings, Amount = 8_000_000, TransferDate = D(2025,6,6),  Description = "Gửi tiết kiệm tháng 6" },
                // T6: Nạp MoMo
                new Transfer { Source = u1TPBank, Destination = u1MoMo,    Amount = 500_000,   TransferDate = D(2025,6,20), Description = "Nạp ví MoMo tháng 6" },

                // User 2: chuyển từ Techcombank sang ZaloPay
                new Transfer { Source = u2Techcombank, Destination = u2ZaloPay, Amount = 200_000, TransferDate = D(2025,4,10), Description = "Nạp ZaloPay" },
                new Transfer { Source = u2Techcombank, Destination = u2Cash,    Amount = 500_000, TransferDate = D(2025,5,20), Description = "Rút tiền mặt" },

                // User 3: chuyển nhỏ
                new Transfer { Source = u3VietinBank, Destination = u3Cash, Amount = 300_000, TransferDate = D(2025,5,10), Description = "Rút tiền tiêu vặt" },
            };
            context.Transfers.AddRange(transfers);

            // ================================================================
            // 8. ĐIỀU CHỈNH SỐ DƯ – ADJUST BALANCE (User 1)
            // ================================================================
            var adjustments = new List<AdjustBalance>
            {
                // Kiểm đếm lại tiền mặt đầu tháng 2
                new AdjustBalance
                {
                    Account    = u1Cash,
                    Amount = -150_000,
                    CreatedAt = D(2025,2,1),
                },
                // Phát hiện lỗi giao dịch tháng 3
                new AdjustBalance
                {
                    Account    = u1TPBank,
                    Amount = 150_000,
                    CreatedAt = D(2025,3,20),
                },
                // Điều chỉnh MoMo sau kiểm tra
                new AdjustBalance
                {
                    Account    = u1MoMo,
                    Amount = 20_000,
                    CreatedAt = D(2025,4,15),
                },
                // Điều chỉnh MBBank – phát hiện phí dịch vụ trừ tự động
                new AdjustBalance
                {
                    Account    = u1MBBank,
                    Amount = -110_000,
                    CreatedAt = D(2025,5,1),
                },
                // Điều chỉnh VCB sau sao kê
                new AdjustBalance
                {
                    Account    = u1VCB,
                    Amount = 500_000,
                    CreatedAt = D(2025,6,1),
                },
                // User 2: điều chỉnh cash
                new AdjustBalance
                {
                    Account    = u2Cash,
                    Amount = -50_000,
                    CreatedAt = D(2025,4,30),
                },
            };
            context.AdjustBalances.AddRange(adjustments);

            // ================================================================
            // LƯU TOÀN BỘ
            // ================================================================
            await context.SaveChangesAsync();
        }

        /// <summary>Tạo DateTime UTC từ năm/tháng/ngày</summary>
        private static DateTime D(int year, int month, int day)
            => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}