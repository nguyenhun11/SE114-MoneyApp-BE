using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SE114_MoneyApp_BE.Controllers.Base;
using SE114_MoneyApp_BE.Data;
using SE114_MoneyApp_BE.DTOs.City;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Controllers
{
    [Route("api/[controller]")]
    public class CityController : AuthorizeControllerBase
    {
        public CityController(AppDbContext context, IMemoryCache cache) : base(context, cache) { }

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

        [HttpPost("build")]
        public async Task<ActionResult> Build(BuildRequest request)
        {
            var (userId, success, message) = GetCurrentUserId();
            if (!success) return Unauthorized(new { Message = message });

            var city = await _context.CityStates.FirstOrDefaultAsync(c => c.UserId == userId);
            if (city == null) return BadRequest("City not found");

            // Chi phí xây dựng cơ bản: 100 PP
            int cost = 100;
            if (city.ProsperityPoints < cost)
            {
                return BadRequest(new { Message = "Insufficient Prosperity Points" });
            }

            city.ProsperityPoints -= cost;

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

            return Ok(new { Message = "Xây dựng thành công", RemainingProsperity = city.ProsperityPoints });
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
