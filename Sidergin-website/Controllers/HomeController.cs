using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sidergin_website.Models;
using Sidergin_website.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Sidergin_website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(Contact contact)
        {
            if (!ModelState.IsValid)
            {
                return View(contact);
            }

            try
            {
                // Đặt thời gian tạo là thời điểm hiện tại
                contact.CreatedAt = DateTime.UtcNow;

                // Thêm thông tin liên hệ vào database
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                // Ghi log
                _logger.LogInformation($"Đã lưu thông tin liên hệ: ContactId={contact.ContactId}");

                // Thêm thông báo thành công vào TempData
                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi tin nhắn! Chúng tôi sẽ liên hệ lại trong thời gian sớm nhất.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông tin liên hệ");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại sau.";
                return View(contact);
            }

            return RedirectToAction("Contact");
        }

        public IActionResult Checkout()
        {
            return View();
        }

        public IActionResult News()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}