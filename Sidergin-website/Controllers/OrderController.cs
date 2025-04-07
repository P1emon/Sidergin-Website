using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sidergin_website.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Retail()
        {
            return View();
        }
        [Authorize]
        public IActionResult PaymentCallBack()
        {
            return View();
        }
    }
}
