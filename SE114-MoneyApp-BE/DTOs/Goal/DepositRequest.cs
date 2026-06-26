using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.DTOs.Goal
{
    public class DepositRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Deposit amount must be greater than 0")]
        public decimal Amount { get; set; }
        [Required] public Guid AccountId { get; set; }
    }
}
