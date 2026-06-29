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

            if (userQuest.Quest!.RewardType == RewardType.SP)
            {
                city.StabilityPoints += userQuest.Quest.RewardPoints;
                city.TotalStabilityPoints += userQuest.Quest.RewardPoints; // Cộng vào tổng điểm tích lũy
            }
            else
            {
                city.ProsperityPoints += userQuest.Quest.RewardPoints;
                city.TotalProsperityPoints += userQuest.Quest.RewardPoints; // Cộng vào tổng điểm tích lũy
            }

            userQuest.IsClaimed = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Reward claimed successfully",
                NewSP = city.StabilityPoints,
                NewPP = city.ProsperityPoints
            });
        }
    }
}
