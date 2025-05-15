using Microsoft.AspNetCore.Mvc;

namespace Prosjektoppgave_218.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
