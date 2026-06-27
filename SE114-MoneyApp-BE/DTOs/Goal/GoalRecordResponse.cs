namespace SE114_MoneyApp_BE.DTOs.Goal
{
    public class GoalRecordResponse
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public string GoalName { get; set; } = string.Empty;
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty; // Để FE hiển thị ví dụ: "Nạp từ Ví Techcombank"
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // "Deposit" hoặc "Withdraw"
        public DateTime CreatedAt { get; set; }
    }

    // Cấu trúc phản hồi sau khi thực hiện hành động Nạp hoặc Rút tiền thành công
    public class GoalTransactionResponse
    {
        public string Message { get; set; } = string.Empty;
        public decimal CurrentAmount { get; set; }
        public decimal Progress { get; set; }
        public decimal AccountAvailableBalance { get; set; } // Trả về số khả dụng mới của ví để FE update ngay
    }

    // Cấu trúc phản hồi sau khi xóa một record lịch sử
    public class GoalRecordDeleteResponse
    {
        public string Message { get; set; } = string.Empty;
        public decimal NewGoalAmount { get; set; }
    }
}
