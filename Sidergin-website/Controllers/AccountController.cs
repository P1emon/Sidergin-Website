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

            try
            {
                // Lưu thông tin người dùng vào database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Trả về thông báo lỗi nếu lưu thất bại
                return Json(new { success = false, message = "Lỗi khi lưu thông tin người dùng: " + ex.Message });
            }

            // Gửi mật khẩu đến email
            bool emailSent = await SendPasswordToEmail(email, password);
            if (!emailSent)
            {
                // Nếu gửi email thất bại, xóa thông tin người dùng đã lưu
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // Trả về thông báo lỗi
                return Json(new { success = false, message = "Gửi email thất bại!" });
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
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Store userId
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

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
                message.From.Add(new MailboxAddress("Admin", smtpSettings["SenderEmail"]));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "Mật khẩu đăng nhập của bạn";
                message.Body = new TextPart("plain") { Text = $"Mật khẩu của bạn: {password}" };

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
    }
}

    
