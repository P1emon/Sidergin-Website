using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sidergin_website.Data;
using Sidergin_website.DTO;
using Sidergin_website.Models;
using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace Sidergin_website.ApiControllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public OrderApiController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder(OrderDTO orderDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = new Order
            {
                UserId = orderDto.UserId,
                Quantity = orderDto.Quantity,
                CurrentPrice = orderDto.CurrentPrice,
                TotalAmount = orderDto.TotalAmount,
                PaymentMethod = orderDto.PaymentMethod,
                PaymentStatus = orderDto.PaymentStatus,
                OrderStatus = orderDto.OrderStatus,
                Notes = orderDto.Notes,
                VnpayTransactionId = orderDto.VnpayTransactionId,
                OrderDate = orderDto.OrderDate
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Lấy email của user từ database
            var user = await _context.Users.FindAsync(orderDto.UserId);
            string userEmail = user?.Email;

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Gửi email dưới nền (không chặn API)
                Task.Run(() => SendOrderConfirmationEmail(userEmail, order));
            }

            return Ok(new { message = "Đơn hàng đã được tạo thành công!", orderId = order.OrderId });
        }

        private async Task SendOrderConfirmationEmail(string email, Order order, bool isAdmin = false)
        {
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("Email trống, không gửi.");
                return;
            }

            try
            {
                // Lấy thông tin SMTP từ cấu hình
                string smtpServer = _configuration["EmailSettings:SmtpServer"];
                int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                string senderEmail = _configuration["EmailSettings:SenderEmail"];
                string senderPassword = _configuration["EmailSettings:SenderPassword"];

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                };

                string subject = isAdmin ? "Thông báo đơn hàng mới" : "Xác nhận đơn hàng";
                string body = isAdmin ?
                    $"<p>Admin, có đơn hàng mới:</p><p>Mã đơn hàng: {order.OrderId}</p><p>Người mua: {order.UserId}</p><p>Số lượng: {order.Quantity}</p><p>Tổng tiền: {order.TotalAmount}</p><p>Phương thức thanh toán: {order.PaymentMethod}</p><p>Trạng thái đơn hàng: {order.OrderStatus}</p><p>Ghi chú: {order.Notes}</p>" :
                    $"<p>Chào bạn,</p><p>Đơn hàng của bạn đã được xác nhận.</p><p>Mã đơn hàng: {order.OrderId}</p><p>Số lượng: {order.Quantity}</p><p>Tổng tiền: {order.TotalAmount}</p><p>Phương thức thanh toán: {order.PaymentMethod}</p><p>Trạng thái đơn hàng: {order.OrderStatus}</p><p>Ghi chú: {order.Notes}</p>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                Console.WriteLine($"Gửi email tới: {email}");
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("Email đã được gửi thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            }
        }
    }
}
