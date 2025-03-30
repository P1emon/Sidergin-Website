﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sidergin_website.Data;
using Sidergin_website.DTO;
using Sidergin_website.Models;
using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Numerics;

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

            // Lấy email từ database
            var user = await _context.Users.FindAsync(orderDto.UserId);
            string userEmail = user?.Email;
            string phone = user?.Phone ?? "Không có số điện thoại";

            // Sử dụng tên khách hàng từ OrderDTO (được gửi từ view)
            string userName = orderDto.UserName ?? "Khách hàng";

            if (!string.IsNullOrEmpty(userEmail))
            {
                Task.Run(() => SendOrderConfirmationEmail(userEmail, order, phone, userName));
            }

            return Ok(new { message = "Đơn hàng đã được tạo thành công! Nhân viên sẽ sớm liên hệ với bạn." });
        }

        private async Task SendOrderConfirmationEmail(string email, Order order, string phone, string userName, bool isAdmin = false)
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
                string body = isAdmin
                    ? $@"<h2>📢 Thông báo đơn hàng mới cần xử lý</h2>
                        <p>Xin chào <strong>Nhân viên chăm sóc khách hàng</strong>,</p>
                        <p>Một đơn hàng mới vừa được tạo. Vui lòng liên hệ với khách hàng trong thời gian sớm nhất để xác nhận thông tin và hỗ trợ quá trình đặt hàng.</p>
                        <hr>
                        <p><strong>🛒 Mã đơn hàng:</strong> {order.OrderId}</p>
                        <p><strong>👤 Khách hàng:</strong> {userName}</p>
                        <p><strong>📧 Email:</strong> {email}</p>
                        <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                        <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                        <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                        <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                        <p><strong>📌 Trạng thái đơn hàng:</strong> {order.OrderStatus}</p>
                        <p><strong>📝 Ghi chú từ khách hàng:</strong> {order.Notes}</p>
                        <hr>
                        <p>📞 <strong>Hãy gọi ngay cho khách hàng để xác nhận thông tin và hỗ trợ họ hoàn tất đơn hàng.</strong></p>
                        <p>💼 Nếu có bất kỳ vấn đề gì, vui lòng báo cáo lại cho quản lý.</p>
                        <p>Trân trọng,</p>
                        <p><strong>Hệ thống quản lý đơn hàng</strong></p>"

                    : $@"<h2>📢 Thông báo đơn hàng mới cần xử lý</h2>
                        <p>Xin chào <strong>Nhân viên chăm sóc khách hàng</strong>,</p>
                        <p>Một đơn hàng mới vừa được tạo. Vui lòng liên hệ với khách hàng trong thời gian sớm nhất để xác nhận thông tin và hỗ trợ quá trình đặt hàng.</p>
                        <hr>
                        <p><strong>🛒 Mã đơn hàng:</strong> {order.OrderId}</p>
                        <p><strong>👤 Khách hàng:</strong> {userName}</p>
                        <p><strong>📧 Email:</strong> {email}</p>
                        <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                        <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                        <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                        <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                        <p><strong>📌 Trạng thái đơn hàng:</strong> {order.OrderStatus}</p>
                        <p><strong>📝 Ghi chú từ khách hàng:</strong> {order.Notes}</p>
                        <hr>
                        <p>📞 <strong>Hãy gọi ngay cho khách hàng để xác nhận thông tin và hỗ trợ họ hoàn tất đơn hàng.</strong></p>
                        <p>💼 Nếu có bất kỳ vấn đề gì, vui lòng báo cáo lại cho quản lý.</p>
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

                Console.WriteLine($"Gửi email tới: {email}");
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("Email đã được gửi thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            }
        }

        // Bán lẻ - Tương tự như trên
        [HttpPost("create2")]
        public async Task<IActionResult> CreateOrder2(OrderDTO orderDto)
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

            // Lấy email từ database
            var user = await _context.Users.FindAsync(orderDto.UserId);
            string userEmail = user?.Email;
            string phone = user?.Phone ?? "Không có số điện thoại";

            // Sử dụng tên khách hàng từ OrderDTO (được gửi từ view)
            string userName = orderDto.UserName ?? "Khách hàng";

            if (!string.IsNullOrEmpty(userEmail))
            {
                Task.Run(() => SendOrderConfirmationEmail(userEmail, order, phone, userName));
            }

            return Ok(new { message = "Đơn hàng đã được tạo thành công! Nhân viên sẽ sớm liên hệ với bạn." });
        }

        private async Task SendOrderConfirmationEmail2(string email, Order order, string phone, string userName, bool isAdmin = false)
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
                string body = isAdmin
                    ? $@"<h2>📢 Thông báo bán lẻ mới cần xử lý</h2>
                        <p>Xin chào <strong>Nhân viên chăm sóc khách hàng</strong>,</p>
                        <p>Một giao dịch bán lẻ mới vừa được tạo. Vui lòng liên hệ với khách hàng trong thời gian sớm nhất để xác nhận thông tin và hỗ trợ quá trình mua hàng.</p>
                        <hr>
                        <p><strong>🛒 Mã giao dịch:</strong> {order.OrderId}</p>
                        <p><strong>👤 Khách hàng:</strong> {userName}</p>
                        <p><strong>📧 Email:</strong> {email}</p>
                        <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                        <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                        <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                        <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                        <p><strong>📌 Trạng thái:</strong> {order.OrderStatus}</p>
                        <p><strong>✅ Đã thanh toán</strong></p>
                        <p><strong>📝 Ghi chú từ khách hàng:</strong> {order.Notes}</p>
                        <hr>
                        <p>📞 <strong>Hãy gọi ngay cho khách hàng để xác nhận thông tin và hỗ trợ họ hoàn tất giao dịch.</strong></p>
                        <p>💼 Nếu có bất kỳ vấn đề gì, vui lòng báo cáo lại cho quản lý.</p>
                        <p>Trân trọng,</p>
                        <p><strong>Hệ thống quản lý bán lẻ</strong></p>"


                    : $@"<h2>📢 Thông báo bán lẻ mới cần xử lý</h2>
                        <p>Xin chào <strong>Nhân viên chăm sóc khách hàng</strong>,</p>
                        <p>Một giao dịch bán lẻ mới vừa được tạo. Vui lòng liên hệ với khách hàng trong thời gian sớm nhất để xác nhận thông tin và hỗ trợ quá trình mua hàng.</p>
                        <hr>
                        <p><strong>🛒 Mã giao dịch:</strong> {order.OrderId}</p>
                        <p><strong>👤 Khách hàng:</strong> {userName}</p>    
                        <p><strong>📧 Email:</strong> {email}</p>
                        <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                        <p><strong>📦 Số lượng:</strong> {order.Quantity}</p>
                        <p><strong>💰 Tổng tiền:</strong> {order.TotalAmount:C}</p>
                        <p><strong>💳 Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                        <p><strong>📌 Trạng thái:</strong> {order.OrderStatus}</p>
                        <p><strong>✅ Đã thanh toán</strong></p>
                        <p><strong>📝 Ghi chú từ khách hàng:</strong> {order.Notes}</p>
                        <hr>
                        <p>📞 <strong>Hãy gọi ngay cho khách hàng để xác nhận thông tin và hỗ trợ họ hoàn tất giao dịch.</strong></p>
                        <p>💼 Nếu có bất kỳ vấn đề gì, vui lòng báo cáo lại cho quản lý.</p>
                        <p>Trân trọng,</p>
                        <p><strong>Hệ thống quản lý bán lẻ</strong></p>";
                ;

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
