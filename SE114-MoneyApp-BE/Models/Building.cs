using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE114_MoneyApp_BE.Models
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        public int CityStateId { get; set; }

        [Required]
        public string BuildingType { get; set; } = string.Empty;

        public int PositionX { get; set; }

        public int PositionY { get; set; }

        public int Level { get; set; } = 1;

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CityStateId")]
        public CityState? CityState { get; set; }
    }
}
