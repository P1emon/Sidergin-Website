using System;
using System.Collections.Generic;

namespace Sidergin_website.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int Quantity { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentStatus { get; set; }

    public string? OrderStatus { get; set; }

    public string? Notes { get; set; }

    public string? VnpayTransactionId { get; set; }

    public DateTime? OrderDate { get; set; }

    public virtual User User { get; set; } = null!;
}
