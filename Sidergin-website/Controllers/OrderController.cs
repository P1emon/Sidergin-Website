using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sidergin_website.Data;
using Sidergin_website.DTO;
using Sidergin_website.Models;
using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Sidergin_website.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AppDbContext context, IConfiguration configuration, ILogger<OrderController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Checkout(int? quantity, decimal? currentPrice)
        {
            var model = new OrderDTO
            {
                Quantity = quantity.HasValue && quantity > 0 ? quantity.Value : 1,
                CurrentPrice = (quantity.HasValue && quantity > 0 ? quantity.Value : 1) * 400000,
                OrderDate = DateTime.UtcNow,
                PaymentMethod = "COD",
                OrderStatus = "Pending",
                PaymentStatus = "Pending",
                VnpayTransactionId = "COD"
            };

            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    model.UserId = userId;
                    var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
                    if (user != null)
                    {
                        model.UserName = user.Name;
                        model.UserEmail = user.Email;
                        model.UserPhone = user.Phone;

                        // Kiểm tra thông tin đầy đủ
                        if (string.IsNullOrWhiteSpace(user.Name) ||
                            string.IsNullOrWhiteSpace(user.Email) ||
                            string.IsNullOrWhiteSpace(user.Phone))
                        {
                            TempData["ShowProfileAlert"] = true;
                        }
                    }
                }
            }

            return View(model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder(OrderDTO orderDto)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", orderDto);
            }

            // Kiểm tra các trường bắt buộc
            if (orderDto.Quantity <= 0)
            {
                return View("Checkout", orderDto);
            }

            if (orderDto.CurrentPrice <= 0)
            {
                return View("Checkout", orderDto);
            }

            if (string.IsNullOrEmpty(orderDto.PaymentMethod))
            {
                return View("Checkout", orderDto);
            }

            // Xử lý UserId
            int userId = 1;

            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int authenticatedUserId))
                {
                    userId = authenticatedUserId;
                }
            }
            else if (orderDto.UserId > 0)
            {
                userId = orderDto.UserId;
            }
            else
            {
                return View("Checkout", orderDto);
            }

            // Kiểm tra UserId tồn tại
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    return View("Checkout", orderDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra người dùng");
                return StatusCode(500, new { message = "Lỗi khi kiểm tra thông tin người dùng" });
            }

            // Xử lý ngày tháng để tránh lỗi SqlDateTime overflow
            DateTime minSqlDate = new DateTime(1753, 1, 1);
            DateTime orderDate = orderDto.OrderDate == default || orderDto.OrderDate < minSqlDate
                ? DateTime.UtcNow
                : orderDto.OrderDate;

            DateTime? deliveryDate = orderDto.DeliveryDate.HasValue && orderDto.DeliveryDate >= minSqlDate
                ? orderDto.DeliveryDate
                : null;

            // Xử lý PaymentStatus và VnpayTransactionId
            string paymentStatus = "Pending";
            string vnpayTransactionId = "COD";

            if (orderDto.PaymentMethod == "COD")
            {
                paymentStatus = "COD";
            }
            else if (orderDto.PaymentMethod == "VNPAY")
            {
                vnpayTransactionId = orderDto.VnpayTransactionId;
            }

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                Quantity = orderDto.Quantity,
                CurrentPrice = orderDto.CurrentPrice,
                TotalAmount = orderDto.Quantity * orderDto.CurrentPrice,
                PaymentMethod = orderDto.PaymentMethod,
                PaymentStatus = paymentStatus,
                OrderStatus = "Pending",
                Notes = orderDto.Notes,
                VnpayTransactionId = vnpayTransactionId,
                OrderDate = orderDate,
                DeliveryDate = deliveryDate
            };

            // Lưu đơn hàng vào database
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đã tạo đơn hàng thành công: OrderId={order.OrderId}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu đơn hàng vào cơ sở dữ liệu");
                return StatusCode(500, new { message = "Lỗi khi lưu đơn hàng", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi lưu đơn hàng");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu đơn hàng", error = ex.Message });
            }

            // Gửi email xác nhận
            try
            {
                string customerEmail = orderDto.UserEmail;
                string userName = orderDto.UserName ?? "Khách hàng";
                string phone = orderDto.UserPhone ?? "Không có số điện thoại";
                string adminEmail = _configuration["EmailSettings:AdminEmail"] ?? "phannguyendangkhoa0915@gmail.com";

                if (!string.IsNullOrEmpty(customerEmail))
                {
                    await SendCustomerEmail(customerEmail, order, phone, userName);
                }

                await SendAdminEmail(adminEmail, order, phone, userName, customerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email xác nhận");
            }

            // Chuyển hướng đến trang Success thay vì trả về JSON
            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        // Action để hiển thị trang xác nhận đơn hàng
        [HttpGet]
        public IActionResult OrderConfirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        // Action mới để hiển thị trang Success
        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet("GetMyOrders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                // Lấy userId từ claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("Người dùng chưa đăng nhập");

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    return BadRequest("Mã người dùng không hợp lệ");

                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // Trả về view với danh sách đơn hàng
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đơn hàng của người dùng hiện tại");
                return StatusCode(500, "Lỗi máy chủ khi lấy đơn hàng: " + ex.Message);
            }
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

                string orderDateFormatted = order.OrderDate.HasValue
                    ? order.OrderDate.Value.ToString("dd/MM/yyyy HH:mm")
                    : "Chưa xác định";

                string formattedAmount = order.TotalAmount.HasValue
                    ? string.Format("{0:N0} VNĐ", order.TotalAmount.Value)
                    : "Đang cập nhật";

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
                            <p><strong>📌 Trạng thái đơn hàng:</strong> {(string.IsNullOrEmpty(order.OrderStatus) ? "Mới đặt" : order.OrderStatus)}</p>
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
                _logger.LogInformation($"Đã gửi email xác nhận cho khách hàng: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email khách hàng: {email}");
                throw;
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

                string formattedAmount = order.TotalAmount.HasValue
                    ? string.Format("{0:N0} VNĐ", order.TotalAmount.Value)
                    : "Đang cập nhật";

                string subject = "Thông báo đơn hàng mới #" + order.OrderId;
                string body = $@"<h2>📢 Thông báo đơn hàng mới</h2>
                                <p>Xin chào Admin,</p>
                                <p>Một đơn hàng mới vừa được tạo.</p>
                                <hr>
                                <p><strong>🛒 Mã đơn hàng:</strong> {order.OrderId}</p>
                                <p><strong>👤 Khách hàng:</strong> {userName}</p>
                                <p><strong>📧 Email khách hàng:</strong> {customerEmail ?? "Không có"}</p>
                                <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                                <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                                <p><strong>💰 Tổng tiền:</strong> {formattedAmount}</p>
                                <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                                <p><strong>📌 Trạng thái đơn hàng:</strong> {(string.IsNullOrEmpty(order.OrderStatus) ? "Mới đặt" : order.OrderStatus)}</p>
                                <p><strong>📝 Ghi chú từ khách hàng:</strong> {(string.IsNullOrEmpty(order.Notes) ? "Không có" : order.Notes)}</p>
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
                _logger.LogInformation($"Đã gửi email thông báo đơn hàng mới tới admin: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email admin: {email}");
                throw;
            }
        }
    }
}