using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class CityState
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Level { get; set; } = 1;

        public int ProsperityPoints { get; set; } = 0;

        public int StabilityPoints { get; set; } = 0;

        public int TotalProsperityPoints { get; set; } = 0;

        public int TotalStabilityPoints { get; set; } = 0;

        public DateTime? LastCheckIn { get; set; }

        public int CurrentStreak { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}
