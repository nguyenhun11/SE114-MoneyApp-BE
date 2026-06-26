using System.ComponentModel.DataAnnotations;

namespace SE114_MoneyApp_BE.DTOs.Goal
{
    public class WithdrawRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền rút phải lớn hơn 0")]
        public decimal Amount { get; set; }
        [Required] public Guid AccountId { get; set; }
    }
}
