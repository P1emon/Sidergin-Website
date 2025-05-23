﻿namespace Sidergin_website.DTO
{
    public class OrderDTO
    {
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // Giá trị mặc định
        public string OrderStatus { get; set; } = "Pending"; // Giá trị mặc định
        public string Notes { get; set; } = "Không có ghi chú"; // Giá trị mặc định
        public string VnpayTransactionId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; } // Nullable để phù hợp với logic
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string UserPhone { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
