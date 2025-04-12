using Sidergin_website.Models;
using Sidergin_website.DTO;
using Microsoft.EntityFrameworkCore;
using Sidergin_website.Data;

namespace Sidergin_website.Services
{
    public class OrderOperations
    {
        private readonly AppDbContext _context;

        public OrderOperations(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDTO>> GetOrdersWithUserInfoAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Select(o => new OrderDTO
                {
                    UserId = o.UserId,
                    Quantity = o.Quantity,
                    CurrentPrice = o.CurrentPrice,
                    
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus ?? "pending",
                    OrderStatus = o.OrderStatus ?? "pending_payment",
                    Notes = o.Notes,
                    VnpayTransactionId = o.VnpayTransactionId,
                    OrderDate = o.OrderDate ?? DateTime.UtcNow,
                    UserPhone = o.User.Phone,
                    UserEmail = o.User.Email
                })
                .ToListAsync();

            return orders;
        }
    }
}
