using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.Quest;
using SE114_MoneyApp_BE.Models;
using SE114_MoneyApp_BE.Services;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class QuestsController : AuthorizeControllerBase
    {
        private readonly GamificationService _gamificationService;

        public QuestsController(AppDbContext context, IMemoryCache cache, GamificationService gamificationService)
            : base(context, cache)
        {
            _gamificationService = gamificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuestResponse>>> GetQuests()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var today = DateTime.UtcNow.Date;

            // Lấy danh sách nhiệm vụ của user trong ngày hôm nay
            var userQuests = await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId && uq.CreatedAt.Date == today)
                .ToListAsync();

            // Nếu chưa có nhiệm vụ cho hôm nay, khởi tạo từ danh sách nhiệm vụ mẫu
            if (!userQuests.Any())
            {
                var allQuests = await _context.Quests.ToListAsync();
                foreach (var quest in allQuests)
                {
                    var newUserQuest = new UserQuest
                    {
                        UserId = userId,
                        QuestId = quest.Id,
                        CurrentProgress = 0,
                        IsCompleted = false,
                        IsClaimed = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserQuests.Add(newUserQuest);
                }
                await _context.SaveChangesAsync();

                // Lấy lại danh sách sau khi lưu
                userQuests = await _context.UserQuests
                    .Include(uq => uq.Quest)
                    .Where(uq => uq.UserId == userId && uq.CreatedAt.Date == today)
                    .ToListAsync();
            }

            var response = userQuests.Select(uq => new QuestResponse
            {
                Id = uq.QuestId,
                Title = uq.Quest!.Title,
                Description = uq.Quest.Description,
                Target = uq.Quest.Target,
                CurrentProgress = uq.CurrentProgress,
                RewardPoints = uq.Quest.RewardPoints,
                RewardType = uq.Quest.RewardType,
                IsCompleted = uq.IsCompleted,
                IsClaimed = uq.IsClaimed
            });

            return Ok(response);
        }

        [HttpPost("claim/{id}")]
        public async Task<IActionResult> ClaimReward(string id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var today = DateTime.UtcNow.Date;
            var userQuest = await _context.UserQuests
                .Include(uq => uq.Quest)
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == id && uq.CreatedAt.Date == today);

            if (userQuest == null) return NotFound(new { Message = "Quest not found for today" });
            if (!userQuest.IsCompleted) return BadRequest(new { Message = "Quest is not completed yet" });
            if (userQuest.IsClaimed) return BadRequest(new { Message = "Reward already claimed" });

            // Cộng điểm thưởng
            var city = await _context.CityStates.FirstOrDefaultAsync(c => c.UserId == userId);
            if (city == null) return BadRequest(new { Message = "City state not initialized" });

            int baseReward = userQuest.Quest!.RewardPoints;
            int bonusPP = 0;
            int bonusSP = 0;
            int totalReward = baseReward;

            // Nếu là thưởng PP, tính bonus PP từ Cửa hàng (Shop)
            if (userQuest.Quest.RewardType == RewardType.PP)
            {
                bonusPP = await _gamificationService.GetBuildingBonus(userId, "shop", 50);
                totalReward += bonusPP;

                city.ProsperityPoints += totalReward;
                city.TotalProsperityPoints += totalReward;

                userQuest.IsClaimed = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Reward claimed successfully",
                    basePP = baseReward,
                    bonusPP = bonusPP,
                    totalPP = totalReward
                });
            }
            else
            {
                // Nếu là thưởng SP, tính bonus SP từ Cửa hàng (Shop) như mô tả trong ảnh
                // Và đồng thời vẫn giữ bonus từ Nhà ở (House) nếu muốn,
                // nhưng ở đây tôi ưu tiên bonus từ Shop (+10 SP) theo yêu cầu ảnh UI.
                bonusSP = await _gamificationService.GetBuildingBonus(userId, "shop", 10);
                totalReward += bonusSP;

                city.StabilityPoints += totalReward;
                city.TotalStabilityPoints += totalReward;

                userQuest.IsClaimed = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Reward claimed successfully",
                    baseSP = baseReward,
                    bonusSP = bonusSP,
                    totalSP = totalReward
                });
            }
        }
    }
}
