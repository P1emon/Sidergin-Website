using Microsoft.AspNetCore.Mvc;
using Sidergin_website.Data;

namespace Sidergin_website.Controllers
{
    public class FaqController : Controller
    {
        private readonly AppDbContext _context;

        public FaqController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var faqs = _context.Fqas.OrderBy(f => f.CreatedAt).ToList();
            return View(faqs);
        }
    }
}
