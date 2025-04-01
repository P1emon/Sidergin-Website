using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Sidergin_website.Data;
using Sidergin_website.Models;
using Microsoft.EntityFrameworkCore;

namespace Sidergin_website.Controllers
{

    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public AccountController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public IActionResult Index()
        {

            return View();
        }
        [HttpPost]

        public async Task<IActionResult> Register(string email)
        {
            // Kiểm tra email đã tồn tại chưa
            if (_context.Users.Any(u => u.Email == email))
            {
                return Json(new { success = false, message = "Email đã tồn tại!" });
            }

            // Tạo mật khẩu ngẫu nhiên và mã hóa
            string password = GenerateRandomPassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Tạo đối tượng người dùng mới
            var user = new User { Email = email, Password = hashedPassword };

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lưu thông tin người dùng vào database
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Gửi mật khẩu đến email
                    bool emailSent = await SendPasswordToEmail(email, password);
                    if (!emailSent)
                    {
                        // Nếu gửi email thất bại, rollback transaction
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = "Gửi email thất bại!" });
                    }

                    // Commit transaction nếu mọi thứ thành công
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    await transaction.RollbackAsync();

                    // Log InnerException để xem chi tiết lỗi
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = $"Lỗi khi lưu thông tin người dùng: {innerException}" });
                }
            }

            // Trả về kết quả thành công
            return Json(new { success = true });
        }



        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email không tồn tại!");
                    return View(model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    ModelState.AddModelError(string.Empty, "Mật khẩu không đúng!");
                    return View(model);
                }

                // Add userId to claims
                var claims = new List<Claim>
        {

            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), 

            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Lưu session đăng nhập
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.Email); // Có thể thay bằng user.Name nếu có

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }


        private async Task<bool> SendPasswordToEmail(string email, string password)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Nhà Thuốc", smtpSettings["SenderEmail"]));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "Thông tin đăng nhập - Nhà Thuốc";

                // Create HTML body with better formatting
                var builder = new BodyBuilder();

                // HTML version of the email
                builder.HtmlBody = $@"
        <!DOCTYPE html>
        <html lang='vi'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Thông tin đăng nhập</title>
            <style>
                body {{
                    font-family: 'Segoe UI', Arial, sans-serif;
                    line-height: 1.6;
                    color: #333333;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #ffffff;
                }}
                .header {{
                    background: linear-gradient(135deg, #6e5ff8 0%, #7d4de3 100%);
                    color: white;
                    padding: 20px;
                    text-align: center;
                    border-radius: 5px 5px 0 0;
                }}
                .content {{
                    padding: 20px;
                    border: 1px solid #e0e0e0;
                    border-top: none;
                    border-radius: 0 0 5px 5px;
                }}
                .password-box {{
                    background-color: #f5f5f5;
                    padding: 15px;
                    margin: 20px 0;
                    border-radius: 5px;
                    border-left: 4px solid #6e5ff8;
                    font-size: 16px;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #6e5ff8 0%, #7d4de3 100%);
                    color: white;
                    text-decoration: none;
                    padding: 12px 25px;
                    border-radius: 50px;
                    margin-top: 15px;
                    font-weight: bold;
                }}
                .footer {{
                    margin-top: 20px;
                    text-align: center;
                    font-size: 12px;
                    color: #666666;
                }}
                .warning {{
                    color: #e74c3c;
                    font-size: 13px;
                    margin-top: 10px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Thông Tin Đăng Nhập</h1>
                </div>
                <div class='content'>
                    <p>Kính gửi Quý khách,</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản tại Nhà Thuốc của chúng tôi. Dưới đây là thông tin đăng nhập của bạn:</p>
                    
                    <p><strong>Email:</strong> {email}</p>
                    <div class='password-box'>
                        <strong>Mật khẩu:</strong> {password}
                    </div>
                    
                    <p>Vui lòng sử dụng thông tin này để đăng nhập vào tài khoản của bạn. Sau khi đăng nhập, chúng tôi khuyên bạn nên thay đổi mật khẩu để đảm bảo an toàn.</p>
                    
                    <p>Để đăng nhập ngay bây giờ, vui lòng nhấp vào nút bên dưới:</p>
                    
                    <div style='text-align: center;'>
                        <a href='{_configuration["ApplicationUrl"]}/Account/Login' class='button'>Đăng Nhập</a>
                    </div>
                    
                    <p class='warning'>Lưu ý: Đây là email tự động. Vui lòng không trả lời email này.</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} Nhà Thuốc. Tất cả các quyền đã được bảo lưu.</p>
                    <p>Địa chỉ: {_configuration["CompanyInfo:Address"] ?? "123 Đường Nguyễn Văn Linh, Quận 7, TP.HCM"}</p>
                    <p>Hotline: {_configuration["CompanyInfo:Phone"] ?? "1900 1234"}</p>
                </div>
            </div>
        </body>
        </html>";

                // Plain text version as fallback
                builder.TextBody = $@"
        THÔNG TIN ĐĂNG NHẬP - NHÀ THUỐC
        
        Kính gửi Quý khách,
        
        Cảm ơn bạn đã đăng ký tài khoản tại Nhà Thuốc của chúng tôi. Dưới đây là thông tin đăng nhập của bạn:
        
        Email: {email}
        Mật khẩu: {password}
        
        Vui lòng sử dụng thông tin này để đăng nhập vào tài khoản của bạn. Sau khi đăng nhập, chúng tôi khuyên bạn nên thay đổi mật khẩu để đảm bảo an toàn.
        
        Để đăng nhập, vui lòng truy cập: {_configuration["ApplicationUrl"]}/Account/Login
        
        Lưu ý: Đây là email tự động. Vui lòng không trả lời email này.
        
        © {DateTime.Now.Year} Nhà Thuốc. Tất cả các quyền đã được bảo lưu.
        Địa chỉ: {_configuration["CompanyInfo:Address"] ?? "123 Đường Nguyễn Văn Linh, Quận 7, TP.HCM"}
        Hotline: {_configuration["CompanyInfo:Phone"] ?? "1900 1234"}";

                message.Body = builder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(smtpSettings["SmtpServer"], int.Parse(smtpSettings["SmtpPort"]), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpSettings["SenderEmail"], smtpSettings["SenderPassword"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                return false;
            }
        }
        [HttpGet]
        public IActionResult Logoutt()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (HttpContext.Session != null) // Kiểm tra null
            {
                await HttpContext.Session.CommitAsync();
                HttpContext.Session.Clear();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa tất cả cookies liên quan
            foreach (var cookie in HttpContext.Request.Cookies.Keys)
            {
                if (cookie.StartsWith(".AspNetCore."))
                    Response.Cookies.Delete(cookie, new CookieOptions { Secure = true });
            }

            // Hủy session server-side
            await HttpContext.Session.CommitAsync();
            HttpContext.Session.Clear();

            // Redirect an toàn
            return RedirectToAction("PostLogout", "Account");
        }

        [HttpGet]
        public IActionResult PostLogout()
        {
            // Ngăn chặn back button
            Response.Headers["Cache-Control"] = "no-cache, no-store";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View(); // Trang thông báo đăng xuất thành công
        }





        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null) return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User model, string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin cá nhân
            user.Name = model.Name;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.UpdatedAt = DateTime.Now;

            // Kiểm tra và đổi mật khẩu nếu nhập mật khẩu mới
            if (!string.IsNullOrEmpty(CurrentPassword) && !string.IsNullOrEmpty(NewPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.Password))
                {
                    ViewData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                    return View(model);
                }

                if (NewPassword != ConfirmNewPassword)
                {
                    ViewData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp!";
                    return View(model);
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            ViewData["Message"] = "Cập nhật thành công!";
            return View(model);
        }


    }
}


