using System.Diagnostics;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Support()
        {
            return View();

        }
        public IActionResult About()
        {
            return View();

        }

        public IActionResult FAQ()
        {
            return View();

        }
        public IActionResult Contact()
        {
            return View();

        }


        public IActionResult Service()
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
