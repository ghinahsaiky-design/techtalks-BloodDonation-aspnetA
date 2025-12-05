using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
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

        // CONTACT – GET
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // CONTACT – POST (sends email)
        [HttpPost]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Read settings from appsettings.json
            string smtpServer = _config["EmailSettings:SmtpServer"];
            int port = int.Parse(_config["EmailSettings:Port"]);
            string username = _config["EmailSettings:Username"];
            string password = _config["EmailSettings:Password"];
            string receiverEmail = _config["EmailSettings:ReceiverEmail"];

            try
            {
                var fromAddress = new MailAddress(username, "BloodConnect Contact Form");
                var toAddress = new MailAddress(receiverEmail);

                using (var message = new MailMessage())
                {
                    message.From = fromAddress;
                    message.To.Add(toAddress);

                    message.Subject = $"New message from {model.FullName}";
                    message.Body =
                        $"From: {model.FullName} ({model.Email})\n" +
                        $"Phone: {model.Phone}\n\n" +
                        $"Message:\n{model.Message}";

                    // Replies go to the user's email
                    message.ReplyToList.Add(new MailAddress(model.Email));

                    using (var smtp = new SmtpClient(smtpServer, port))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(username, password);
                        await smtp.SendMailAsync(message);
                    }
                }

                // SUCCESS
                ViewBag.Success = "Your message has been sent. Thank you for contacting us!";
                ModelState.Clear();
            }
            catch (Exception ex)
            {
                // ERROR
                _logger.LogError(ex, "Error sending contact email.");
                // General error not linked to a specific field
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }

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