namespace Sidergin_website.DTO
{
    public class OrderDTO
    {
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = "pending";
        public string OrderStatus { get; set; } = "pending_payment";
        public string? Notes { get; set; }
        public string? VnpayTransactionId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        // Thêm trường để nhận tên từ view
        public string UserName { get; set; }
        public string UserPhone { get; set; }
        public string UserEmail { get; set; }
    }
}
