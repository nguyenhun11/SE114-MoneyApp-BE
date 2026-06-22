using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Services
{
    public class GamificationService
    {
        private readonly AppDbContext _context;

        public GamificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnDailyCheckIn(int userId, DateTime? clientDate = null)
        {
            var city = await GetOrCreateCityState(userId);
            var checkInDate = clientDate?.Date ?? DateTime.UtcNow.Date;

            // Kiểm tra xem ngày yêu cầu đã check-in chưa
            if (city.LastCheckIn == null || city.LastCheckIn.Value.Date < checkInDate)
            {
                // Cập nhật chuỗi (streak)
                if (city.LastCheckIn != null && city.LastCheckIn.Value.Date == checkInDate.AddDays(-1))
                {
                    city.CurrentStreak++;
                }
                else
                {
                    city.CurrentStreak = 1;
                }

                city.StabilityPoints += 10; // Cộng 10 điểm cho check-in hàng ngày
                city.LastCheckIn = checkInDate;

                await CheckLevelUp(city);
                await _context.SaveChangesAsync();
            }
        }

        public async Task OnTransactionAdded(int userId, DateTime transactionDate)
        {
            var city = await GetOrCreateCityState(userId);
            var txDate = transactionDate.Date;

            // Nếu giao dịch thuộc về một ngày chưa được check-in, thực hiện check-in cho ngày đó
            if (city.LastCheckIn == null || city.LastCheckIn.Value.Date < txDate)
            {
                await OnDailyCheckIn(userId, txDate);
                // Sau khi check-in cho ngày txDate, LastCheckIn của city sẽ là txDate
                // Chúng ta không cộng thêm 1đ cho giao dịch đầu tiên của ngày mới này
                return;
            }

            // Nếu giao dịch cùng ngày với LastCheckIn, hoặc cũ hơn (nhưng đã check-in ngày đó rồi)
            // thì chỉ cộng 1 điểm SP. Lưu ý: Logic này giả định LastCheckIn luôn tăng tiến.
            city.StabilityPoints += 1;

            await CheckLevelUp(city);
            await _context.SaveChangesAsync();
        }

        public async Task OnGoalCompleted(int userId)
        {
            var city = await GetOrCreateCityState(userId);
            city.ProsperityPoints += 100; // Thưởng lớn khi hoàn thành mục tiêu tiết kiệm

            await CheckLevelUp(city);
            await _context.SaveChangesAsync();
        }

        public async Task OnBudgetMaintained(int userId, decimal savedAmount)
        {
            var city = await GetOrCreateCityState(userId);
            // Cộng điểm dựa trên số tiền tiết kiệm được so với ngân sách (ví dụ: 1 điểm cho mỗi 100k)
            int points = (int)(savedAmount / 100000);
            city.ProsperityPoints += Math.Max(10, points);

            await CheckLevelUp(city);
            await _context.SaveChangesAsync();
        }

        private async Task<CityState> GetOrCreateCityState(int userId)
        {
            var city = await _context.CityStates
                .Include(c => c.Buildings)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (city == null)
            {
                city = new CityState
                {
                    UserId = userId,
                    Level = 1,
                    ProsperityPoints = 0,
                    StabilityPoints = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CityStates.Add(city);
                await _context.SaveChangesAsync();
            }

            return city;
        }

        private Task CheckLevelUp(CityState city)
        {
            // Công thức thăng cấp: Level * 500 điểm Prosperity
            int requiredPoints = city.Level * 500;
            if (city.ProsperityPoints >= requiredPoints)
            {
                city.Level++;
                // Có thể mở rộng thêm logic thông báo thăng cấp ở đây
            }
            return Task.CompletedTask;
        }
    }
}
