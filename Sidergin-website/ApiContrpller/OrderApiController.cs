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
using System.Security.Claims;

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

            // Get userId from authenticated user if available, otherwise use DTO value
            int userId = orderDto.UserId;
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int authenticatedUserId))
                {
                    userId = authenticatedUserId;
                }
            }

            // Validate userId exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User không tồn tại" });
            }

            var order = new Order
            {
                UserId = userId,
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

            // Email sending logic remains the same
            string customerEmail = orderDto.UserEmail;
            string userName = orderDto.UserName ?? "Khách hàng";
            string phone = orderDto.UserPhone ?? "Không có số điện thoại";
            string adminEmail = "phannguyendangkhoa0915@gmail.com";

            if (!string.IsNullOrEmpty(customerEmail))
            {
                Task.Run(() => SendCustomerEmail(customerEmail, order, phone, userName));
            }

            Task.Run(() => SendAdminEmail(adminEmail, order, phone, userName, customerEmail));

            return Ok(new { message = "Đơn hàng đã được tạo thành công! Nhân viên sẽ sớm liên hệ với bạn." });
        }

        private async Task SendCustomerEmail(string email, Order order, string phone, string userName)
        {
            if (string.IsNullOrEmpty(email)) return;

            try
            {
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

                string subject = "Xác nhận đơn hàng";
                string body = $@"<h2>📢 Xác nhận đơn hàng</h2>
                                <p>Xin chào {userName},</p>
                                <p>Đơn hàng của bạn đã được xác nhận thành công.</p>
                                <hr>
                                <p><strong>🛒 Mã đơn hàng:</strong> {order.OrderId}</p>
                                <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                                <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                                <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                                <p><strong>📌 Trạng thái đơn hàng:</strong> {order.OrderStatus}</p>
                                <p><strong>📝 Ghi chú:</strong> {order.Notes}</p>
                                <hr>
                                <p>📞 Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ chúng tôi.</p>
                                <p>Trân trọng,</p>
                                <p><strong>Hệ thống quản lý đơn hàng</strong></p>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email khách hàng: {ex.Message}");
            }
        }

        private async Task SendAdminEmail(string email, Order order, string phone, string userName, string customerEmail)
        {
            if (string.IsNullOrEmpty(email)) return;

            try
            {
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

                string subject = "Thông báo đơn hàng mới";
                string body = $@"<h2>📢 Thông báo đơn hàng mới</h2>
                                <p>Xin chào Admin,</p>
                                <p>Một đơn hàng mới vừa được tạo.</p>
                                <hr>
                                <p><strong>🛒 Mã đơn hàng:</strong> {order.OrderId}</p>
                                <p><strong>👤 Khách hàng:</strong> {userName}</p>
                                <p><strong>📧 Email khách hàng:</strong> {customerEmail}</p>
                                <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                                <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                                <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                                <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                                <p><strong>📌 Trạng thái đơn hàng:</strong> {order.OrderStatus}</p>
                                <p><strong>📝 Ghi chú từ khách hàng:</strong> {order.Notes}</p>
                                <hr>
                                <p>📞 Hãy liên hệ với khách hàng sớm nhất.</p>
                                <p>Trân trọng,</p>
                                <p><strong>Hệ thống quản lý đơn hàng</strong></p>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email admin: {ex.Message}");
            }
        }
    }
}
