using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.City;
using SE114_MoneyApp_BE.Models;

using SE114_MoneyApp_BE.Services;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class CityController : AuthorizeControllerBase
    {
        private readonly GamificationService _gamificationService;

        public CityController(AppDbContext context, IMemoryCache cache, GamificationService gamificationService)
            : base(context, cache)
        {
            _gamificationService = gamificationService;
        }

        [HttpGet]
        public async Task<ActionResult<CityResponse>> GetCity()
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var city = await _context.CityStates
                .Include(c => c.Buildings)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (city == null)
            {
                city = new CityState { UserId = userId };
                _context.CityStates.Add(city);
                await _context.SaveChangesAsync();
            }

            return Ok(new CityResponse
            {
                Level = city.Level,
                ProsperityPoints = city.ProsperityPoints,
                StabilityPoints = city.StabilityPoints,
                CurrentStreak = city.CurrentStreak,
                Buildings = city.Buildings.Select(b => new BuildingDto
                {
                    Id = b.Id,
                    BuildingType = b.BuildingType,
                    PositionX = b.PositionX,
                    PositionY = b.PositionY,
                    Level = b.Level
                }).ToList()
            });
        }

        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<RankingResponse>>> GetRanking([FromQuery] int type, [FromQuery] int limit = 50)
        {
            IQueryable<CityState> query = _context.CityStates.Include(c => c.User);

            if (type == 1)
            {
                // Xếp hạng theo TỔNG điểm Prosperity tích lũy
                query = query.OrderByDescending(c => c.TotalProsperityPoints);
            }
            else if (type == 2)
            {
                // Xếp hạng theo TỔNG điểm Stability tích lũy
                query = query.OrderByDescending(c => c.TotalStabilityPoints);
            }
            else
            {
                return BadRequest(new { Message = "Invalid type. Use 1 for Prosperity or 2 for Stability." });
            }

            var rankings = await query.Take(limit).ToListAsync();

            var response = rankings.Select((c, index) => new RankingResponse
            {
                Rank = index + 1,
                UserId = c.UserId,
                Name = c.User?.Name ?? "Unknown",
                ImageUrl = c.User?.ImageUrl,
                ProsperityPoints = type == 1 ? c.TotalProsperityPoints : c.ProsperityPoints,
                StabilityPoints = type == 2 ? c.TotalStabilityPoints : c.StabilityPoints,
                CityLevel = c.Level
            }).ToList();

            return Ok(response);
        }

        [HttpPost("build")]
        public async Task<ActionResult> Build(BuildRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var city = await _context.CityStates.FirstOrDefaultAsync(c => c.UserId == userId);
            if (city == null) return BadRequest("City not found");

            int cost = 0;
            bool useSP = false;

            // Xác định chi phí và loại điểm sử dụng dựa trên BuildingType
            switch (request.BuildingType.ToLower())
            {
                case "bench": cost = 5; useSP = true; break;
                case "road": cost = 10; useSP = true; break;
                case "flower_bed": cost = 10; useSP = true; break;
                case "street_light": cost = 15; useSP = true; break;
                case "tree": cost = 20; useSP = true; break;
                case "park": cost = 50; useSP = true; break;
                case "fountain": cost = 80; useSP = true; break;
                case "statue": cost = 150; useSP = true; break;
                default:
                    cost = 100; // Chi phí PP cho các công trình thông thường
                    useSP = false;
                    break;
            }

            if (useSP)
            {
                if (city.StabilityPoints < cost)
                {
                    return BadRequest(new { Message = "Không đủ điểm Stability (SP)" });
                }
                city.StabilityPoints -= cost;
            }
            else
            {
                if (city.ProsperityPoints < cost)
                {
                    return BadRequest(new { Message = "Không đủ điểm Prosperity (PP)" });
                }
                city.ProsperityPoints -= cost;
            }

            var building = new Building
            {
                CityStateId = city.Id,
                BuildingType = request.BuildingType,
                PositionX = request.PositionX,
                PositionY = request.PositionY,
                Level = 1,
                PurchasedAt = DateTime.UtcNow
            };

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            // Cập nhật tiến độ nhiệm vụ xây dựng và kiểm tra huy hiệu
            await _gamificationService.OnBuildUpgrade(userId);

            return Ok(new
            {
                Message = "Xây dựng thành công",
                RemainingProsperity = city.ProsperityPoints,
                RemainingStability = city.StabilityPoints
            });
        }

        [HttpPost("upgrade/{id}")]
        public async Task<ActionResult> Upgrade(int id)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var city = await _context.CityStates.FirstOrDefaultAsync(c => c.UserId == userId);
            if (city == null) return BadRequest("City not found");

            var building = await _context.Buildings
                .FirstOrDefaultAsync(b => b.Id == id && b.CityStateId == city.Id);

            if (building == null) return NotFound(new { Message = "Building not found" });

            // Kiểm tra nếu là vật phẩm trang trí thì không cho nâng cấp
            var decorativeTypes = new[] { "road", "tree", "park", "fountain", "bench", "street_light", "flower_bed", "statue" };
            if (decorativeTypes.Contains(building.BuildingType.ToLower()))
            {
                return BadRequest(new { Message = "Vật phẩm trang trí không thể nâng cấp" });
            }

            int currentLevel = building.Level;
            int upgradeCost = 0;

            if (currentLevel == 1) upgradeCost = 200;
            else if (currentLevel == 2) upgradeCost = 500;
            else return BadRequest(new { Message = "Maximum level reached" });

            if (city.ProsperityPoints < upgradeCost)
            {
                return BadRequest(new { Message = "Insufficient Prosperity Points" });
            }

            city.ProsperityPoints -= upgradeCost;
            building.Level += 1;

            await _context.SaveChangesAsync();

            // Cập nhật tiến độ nhiệm vụ nâng cấp và kiểm tra huy hiệu
            await _gamificationService.OnBuildUpgrade(userId);

            return Ok(new
            {
                id = building.Id,
                buildingType = building.BuildingType,
                level = building.Level,
                positionX = building.PositionX,
                positionY = building.PositionY
            });
        }
    }
}
