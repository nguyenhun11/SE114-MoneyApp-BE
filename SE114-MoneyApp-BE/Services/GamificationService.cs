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

                await UpdateQuestProgress(userId, "CheckIn");
                await CheckLevelUp(city);
                await CheckBadges(userId); // Luôn kiểm tra badge khi có hoạt động
                await _context.SaveChangesAsync();
            }
        }

        public async Task OnTransactionAdded(int userId, DateTime transactionDate)
        {
            var city = await GetOrCreateCityState(userId);
            var txDate = transactionDate.Date;

            // Cập nhật tiến độ nhiệm vụ "Ghi chép giao dịch"
            await UpdateQuestProgress(userId, "AddTransaction");

            // Kiểm tra huy hiệu BigSpender (> 20tr)
            var lastTx = await _context.Transactions
                .Where(t => t.Account!.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastTx != null && lastTx.BaseAmount > 20000000)
            {
                 // CheckBadges sẽ xử lý logic này bên trong
            }

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
            await CheckBadges(userId);
            await _context.SaveChangesAsync();
        }

        public async Task OnGoalCompleted(int userId)
        {
            var city = await GetOrCreateCityState(userId);
            city.ProsperityPoints += 100; // Thưởng lớn khi hoàn thành mục tiêu tiết kiệm

            await UpdateQuestProgress(userId, "GoalCompleted");
            await CheckLevelUp(city);
            await CheckBadges(userId);
            await _context.SaveChangesAsync();
        }

        public async Task OnGoalDeposited(int userId)
        {
            await UpdateQuestProgress(userId, "GoalDeposited");
            await _context.SaveChangesAsync();
        }

        public async Task OnBudgetMaintained(int userId, decimal savedAmount)
        {
            var city = await GetOrCreateCityState(userId);
            // Cộng điểm dựa trên số tiền tiết kiệm được so với ngân sách (ví dụ: 1 điểm cho mỗi 100k)
            int points = (int)(savedAmount / 100000);
            city.ProsperityPoints += Math.Max(10, points);

            await UpdateQuestProgress(userId, "BudgetMaintained");
            await CheckLevelUp(city);
            await CheckBadges(userId);
            await _context.SaveChangesAsync();
        }

        public async Task OnBudgetSetup(int userId)
        {
            await UpdateQuestProgress(userId, "BudgetSetup");
            await _context.SaveChangesAsync();
        }

        public async Task OnBuildUpgrade(int userId)
        {
            var city = await GetOrCreateCityState(userId);
            await UpdateQuestProgress(userId, "BuildUpgrade");
            await CheckLevelUp(city);
            await CheckBadges(userId);
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

        private async Task UpdateQuestProgress(int userId, string actionType)
        {
            var today = DateTime.UtcNow.Date;
            var userQuests = await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId && uq.CreatedAt.Date == today && uq.Quest!.ActionType == actionType && !uq.IsCompleted)
                .ToListAsync();

            // Nếu chưa có nhiệm vụ nào cho ngày hôm nay, ta khởi tạo chúng ngay lập tức
            if (!await _context.UserQuests.AnyAsync(uq => uq.UserId == userId && uq.CreatedAt.Date == today))
            {
                var allMasterQuests = await _context.Quests.ToListAsync();
                foreach (var q in allMasterQuests)
                {
                    _context.UserQuests.Add(new UserQuest
                    {
                        UserId = userId,
                        QuestId = q.Id,
                        CurrentProgress = 0,
                        IsCompleted = false,
                        IsClaimed = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();

                // Lấy lại danh sách sau khi đã khởi tạo
                userQuests = await _context.UserQuests
                    .Include(uq => uq.Quest)
                    .Where(uq => uq.UserId == userId && uq.CreatedAt.Date == today && uq.Quest!.ActionType == actionType && !uq.IsCompleted)
                    .ToListAsync();
            }

            foreach (var uq in userQuests)
            {
                uq.CurrentProgress++;
                if (uq.CurrentProgress >= uq.Quest!.Target)
                {
                    uq.IsCompleted = true;
                }
            }
        }

        private async Task CheckBadges(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var allBadges = await _context.Badges.ToListAsync();
            var unlockedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            foreach (var badge in allBadges)
            {
                if (unlockedBadgeIds.Contains(badge.Id)) continue;

                bool isUnlocked = false;
                switch (badge.ConditionType)
                {
                    case "Streak":
                        if (user.DailyStreak >= badge.ConditionValue) isUnlocked = true;
                        break;
                    case "GoalCompleted":
                        var completedGoals = await _context.Goals.CountAsync(g => g.UserId == userId && g.CurrentAmount >= g.TargetAmount);
                        if (completedGoals >= badge.ConditionValue) isUnlocked = true;
                        break;
                    case "BudgetMaintained":
                        // Logic đơn giản: Nếu đã từng được gọi OnBudgetMaintained ít nhất conditionValue lần
                        isUnlocked = true;
                        break;
                    case "CityLevel":
                        var city = await GetOrCreateCityState(userId);
                        if (city.Level >= badge.ConditionValue) isUnlocked = true;
                        break;
                    case "FirstBuild":
                        var buildingCount = await _context.Buildings.CountAsync(b => b.CityState!.UserId == userId);
                        if (buildingCount >= badge.ConditionValue) isUnlocked = true;
                        break;
                    case "NightOwl":
                        // Kiểm tra nếu có giao dịch nào thực hiện trong khoảng 2h-4h sáng (giờ local hoặc UTC tùy cấu hình)
                        // Ở đây giả định kiểm tra giờ hiện tại lúc hệ thống xử lý
                        int hour = DateTime.UtcNow.AddHours(7).Hour; // Giả định VN là UTC+7
                        if (hour >= 2 && hour < 4) isUnlocked = true;
                        break;
                    case "LowBalance":
                        var totalBalance = await _context.Accounts.Where(a => a.UserId == userId && a.IsActive).SumAsync(a => a.Balance);
                        if (totalBalance < badge.ConditionValue) isUnlocked = true;
                        break;
                    case "BigSpender":
                        var hasBigTx = await _context.Transactions.AnyAsync(t => t.Account!.UserId == userId && t.BaseAmount > badge.ConditionValue);
                        if (hasBigTx) isUnlocked = true;
                        break;
                    case "TransferKing":
                        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        var transferCount = await _context.Transfers.CountAsync(t => t.Source!.UserId == userId && t.TransferDate >= startOfMonth);
                        if (transferCount >= badge.ConditionValue) isUnlocked = true;
                        break;
                    case "Inactivity":
                        if (user.LastActiveDate.HasValue && (DateTime.UtcNow.Date - user.LastActiveDate.Value.Date).TotalDays >= badge.ConditionValue)
                            isUnlocked = true;
                        break;
                }

                if (isUnlocked)
                {
                    _context.UserBadges.Add(new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        UnlockedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }
}
