using Microsoft.AspNetCore.Mvc;
using Sidergin_website.Services;

namespace Sidergin_website.Controllers
{
    public class NewsController : Controller
    {
        private readonly RssFeedService _rssFeedService;

        public NewsController()
        {
            _rssFeedService = new RssFeedService();
        }

        public async Task<IActionResult> Index(string category = null)
        {
            var allNews = await _rssFeedService.GetAllNewsItemsAsync();

            ViewBag.CurrentCategory = category ?? "all";

            if (!string.IsNullOrEmpty(category))
            {
                allNews = allNews.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(allNews);
        }

    }
}
