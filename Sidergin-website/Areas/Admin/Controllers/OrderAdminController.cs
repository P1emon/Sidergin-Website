using Microsoft.AspNetCore.Mvc;
using Sidergin_website.Services;

namespace Sidergin_website.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderAdminController : Controller
    {
        private readonly OrderOperations _orderService;

        public OrderAdminController(OrderOperations orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetOrdersWithUserInfoAsync();
            return View(orders);
        }
    }
}
