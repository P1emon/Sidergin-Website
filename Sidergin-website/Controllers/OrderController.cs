using Microsoft.AspNetCore.Mvc;

namespace Sidergin_website.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
