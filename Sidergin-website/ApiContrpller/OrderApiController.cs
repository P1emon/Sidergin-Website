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
        private object orderDateFormatted;

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

                // Format ngày đặt hàng theo định dạng Việt Nam
                string orderDateFormatted = order.OrderDate.HasValue
                    ? order.OrderDate.Value.ToString("dd/MM/yyyy HH:mm")
                    : "N/A";  // If orderDate is null, use a default value like "N/A"


                // Format tổng tiền
                string formattedAmount = string.Format("{0:N0} VNĐ", order.TotalAmount);

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                };

                string subject = "Xác nhận đơn hàng #" + order.OrderId + " - SIDERGIN";
                string body = $@"
        <!DOCTYPE html>
        <html lang='vi'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Xác nhận đơn hàng</title>
            <style>
                body, html {{
                    margin: 0;
                    padding: 0;
                    font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
                    color: #333333;
                    line-height: 1.6;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #ffffff;
                }}
                .email-header {{
                    background: linear-gradient(135deg, #6e5ff8 0%, #9271f8 50%, #7d4de3 100%);
                    padding: 30px 20px;
                    text-align: center;
                    color: white;
                    border-radius: 8px 8px 0 0;
                }}
                .email-header h1 {{
                    margin: 0;
                    font-size: 28px;
                    font-weight: 700;
                }}
                .email-body {{
                    padding: 30px 20px;
                    background-color: #f9f9f9;
                    border-left: 1px solid #eeeeee;
                    border-right: 1px solid #eeeeee;
                }}
                .order-info {{
                    background-color: white;
                    border-radius: 8px;
                    padding: 20px;
                    box-shadow: 0 2px 5px rgba(0,0,0,0.05);
                    margin-bottom: 20px;
                }}
                .order-detail {{
                    border-top: 1px solid #eeeeee;
                    border-bottom: 1px solid #eeeeee;
                    padding: 15px 0;
                    margin: 15px 0;
                }}
                .order-detail p {{
                    margin: 8px 0;
                }}
                .highlight {{
                    color: #6e5ff8;
                    font-weight: 600;
                }}
                .email-footer {{
                    background-color: #f9f9f9;
                    padding: 20px;
                    text-align: center;
                    font-size: 14px;
                    color: #666666;
                    border-radius: 0 0 8px 8px;
                    border: 1px solid #eeeeee;
                    border-top: none;
                }}
                .btn {{
                    display: inline-block;
                    background: linear-gradient(135deg, #6e5ff8 0%, #7d4de3 100%);
                    color: white;
                    padding: 12px 25px;
                    text-decoration: none;
                    border-radius: 50px;
                    font-weight: 600;
                    margin-top: 15px;
                }}
                .contact-info {{
                    margin-top: 20px;
                    padding-top: 15px;
                    border-top: 1px solid #eeeeee;
                }}
                .emphasis {{
                    font-weight: 600;
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h1>XÁC NHẬN ĐƠN HÀNG</h1>
                </div>
                <div class='email-body'>
                    <p>Xin chào <span class='emphasis'>{userName}</span>,</p>
                    
                    <p>Cảm ơn bạn đã lựa chọn mua sắm tại <span class='highlight'>SIDERGIN</span>. Chúng tôi vui mừng thông báo rằng đơn hàng của bạn đã được xác nhận thành công. Dưới đây là thông tin chi tiết về đơn hàng của bạn:</p>
                    
                    <div class='order-info'>
                        <h3>🛒 Thông tin đơn hàng #{order.OrderId}</h3>
                        <div class='order-detail'>
                            <p><strong>📅 Ngày đặt hàng:</strong> {orderDateFormatted}</p>
                            <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                            <p><strong>💰 Tổng tiền:</strong> <span class='highlight'>{formattedAmount}</span></p>
                            <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                            <p><strong>📌 Trạng thái đơn hàng:</strong> {order.OrderStatus}</p>
                            {(!string.IsNullOrEmpty(order.Notes) ? $"<p><strong>📝 Ghi chú:</strong> {order.Notes}</p>" : "")}
                        </div>
                    </div>
                    
                    <p>Cảm ơn bạn đã mua sắm tại <span class='highlight'>SIDERGIN</span>. Chúng tôi cam kết cung cấp sản phẩm và dịch vụ chất lượng cao để đáp ứng nhu cầu của bạn. Hy vọng bạn sẽ hài lòng với trải nghiệm mua sắm tại cửa hàng chúng tôi.</p>
                    
                    <p>Nếu bạn có bất kỳ câu hỏi nào về đơn hàng hoặc cần hỗ trợ thêm, vui lòng liên hệ với chúng tôi. Chúng tôi luôn sẵn sàng hỗ trợ bạn.</p>
                    
                    <a href='https://sidergin.com/orders/tracking/{order.OrderId}' class='btn'>Theo dõi đơn hàng</a>
                </div>
                <div class='email-footer'>
                    <div class='contact-info'>
                        <p><strong>SIDERGIN</strong></p>
                        <p>📍 123 Nguyễn Văn Linh, Quận 7, TP.HCM</p>
                        <p>🌐 www.sidergin.com</p>
                        <p>📞 0123 456 789</p>
                        <p>📧 support@sidergin.com</p>
                    </div>
                    <p style='margin-top: 20px; font-size: 12px;'>© 2025 SIDERGIN. Tất cả các quyền được bảo lưu.</p>
                </div>
            </div>
        </body>
        </html>";

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
