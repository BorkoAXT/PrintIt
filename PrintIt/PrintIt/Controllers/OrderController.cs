using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrintIt.Data;
using PrintIt.Models;
using System.Drawing.Imaging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PrintIt.Controllers
{
    public class OrderController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly PaymentsOptions _payments;

        public OrderController(
            ApplicationDbContext context,
            IOptions<PaymentsOptions> payments) : base(context)
        {
            _context = context;
            _payments = payments.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // EPAY PAYMENT
        // =====================================================

        //        public IActionResult PayEpay(int orderId, decimal amount)
        //        {
        //            var epay = _payments.Epay;

        //            var invoice = orderId.ToString();
        //            var expiration = DateTime.UtcNow
        //                .AddHours(1)
        //                .ToString("dd.MM.yyyy");

        //            var data = $@"MIN={epay.Cin}
        //INVOICE={invoice}
        //AMOUNT={amount:F2}
        //EXP_TIME={expiration}
        //DESCR=Order {invoice}";

        //            var encoded = Convert.ToBase64String(
        //                Encoding.UTF8.GetBytes(data));

        //            using var hmac = new HMACSHA1(
        //                Encoding.UTF8.GetBytes(epay.Secret));

        //            var checksum = BitConverter
        //                .ToString(hmac.ComputeHash(
        //                    Encoding.UTF8.GetBytes(encoded)))
        //                .Replace("-", "")
        //                .ToLower();

        //            ViewBag.Endpoint = epay.Endpoint;
        //            ViewBag.Encoded = encoded;
        //            ViewBag.Checksum = checksum;

        //            return View("EpayRedirect");
        //        }

        public IActionResult PayEpay(int orderId, decimal amount)
        {
            // 1. Prepare your data string
            string data = $"MIN={_payments.Epay.Cin}\nINVOICE={orderId}\nAMOUNT={amount.ToString("F2")}\nEXP_TIME={DateTime.Now.AddDays(1):dd.MM.yyyy}";

            // 2. Encode to Base64
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

            // 3. Generate Checksum
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_payments.Epay.Secret));
            string checksum = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(encoded))).Replace("-", "").ToLower();

            // 4. REDIRECT to the Mock Gate with the data in the URL
            return RedirectToAction("MockPaymentGate", new { encoded = encoded, checksum = checksum });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(decimal amount, string email)
        {
            try
            {
                var order = new Order
                {
                    Amount = amount,
                    CustomerEmail = email ?? "guest@example.com", // Fallback
                    Currency = _payments.Currency ?? "EUR",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                // DON'T clear the cart here yet. 
                // Clear it only AFTER the user successfully pays in the Success action.

                return RedirectToAction("PayEpay", new { orderId = order.Id, amount = order.Amount });
            }
            catch (Exception ex)
            {
                // This will stop the redirect and show you the error on screen
                return Content($"Error: {ex.Message} inner: {ex.InnerException?.Message}");
            }
        }

        // This action simulates the external payment page
        [HttpGet]
        public IActionResult MockPaymentGate(string encoded, string checksum)
        {
            // Check if the data actually arrived
            if (string.IsNullOrEmpty(encoded))
            {
                return Content("Error: No encoded data received from the cart.");
            }

            ViewBag.Encoded = encoded;
            ViewBag.Checksum = checksum; // Optional, for display

            return View();
        }

        // This action simulates the notification the bank sends back to your site
        [HttpPost]
        public IActionResult SimulateCallback(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return BadRequest();

            // 1. Decode the Base64 string
            var decodedBytes = Convert.FromBase64String(encoded);
            var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);

            // 2. Extract the Invoice ID (Order.Id)
            // Epay format usually looks like: MIN=123\nINVOICE=456\n...
            var lines = decodedText.Split('\n');
            var invoiceLine = lines.FirstOrDefault(l => l.StartsWith("INVOICE="));

            if (invoiceLine != null)
            {
                var orderIdStr = invoiceLine.Replace("INVOICE=", "").Trim();
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    // 3. Update the Database
                    MarkOrderAsPaid(orderId);
                    return RedirectToAction("Success");
                }
            }

            return Content("Error: Could not find Invoice ID in encoded data.");
        }


        [HttpPost]
        public IActionResult EpayNotification()
        {
            var encoded = Request.Form["encoded"];
            var checksum = Request.Form["checksum"];

            using var hmac = new HMACSHA1(
                Encoding.UTF8.GetBytes(_payments.Epay.Secret));

            var computed = BitConverter
                .ToString(hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(encoded)))
                .Replace("-", "")
                .ToLower();

            if (computed != checksum)
                return Content("ERR=Invalid checksum");

            var decoded = Encoding.UTF8.GetString(
                Convert.FromBase64String(encoded));

            // Example decoded line:
            // INVOICE=1001:STATUS=PAID

            var lines = decoded.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("STATUS=PAID"))
                {
                    var invoicePart = line.Split(':')[0];
                    var orderId = int.Parse(
                        invoicePart.Replace("INVOICE=", ""));

                    MarkOrderAsPaid(orderId);
                }
            }

            return Content("OK");
        }

        // =====================================================
        // MYPOS PAYMENT
        // =====================================================

        public IActionResult PayMyPos(int orderId, decimal amount)
        {
            var mypos = _payments.MyPos;

            var order = orderId.ToString();
            var amountStr = amount.ToString("F2");

            var dataToSign =
                $"{mypos.Sid}{order}{amountStr}{_payments.Currency}";

            var signature = SignMyPos(
                dataToSign,
                mypos.PrivateKey);

            ViewBag.Endpoint = mypos.Endpoint;
            ViewBag.Sid = mypos.Sid;
            ViewBag.Order = order;
            ViewBag.Amount = amountStr;
            ViewBag.Currency = _payments.Currency;
            ViewBag.Wallet = mypos.WalletNumber;
            ViewBag.KeyIndex = mypos.KeyIndex;
            ViewBag.Signature = signature;

            return View("MyPosRedirect");
        }

        private string SignMyPos(string data, string privateKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var bytes = Encoding.UTF8.GetBytes(data);

            var signature = rsa.SignData(
                bytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }

        // =====================================================
        // SUCCESS / CANCEL
        // =====================================================

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }

        // =====================================================
        // ORDER STATUS UPDATE
        // =====================================================

        private void MarkOrderAsPaid(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return;

            order.Status = "Paid";
            order.PaidAt = DateTime.UtcNow;

            // Finding the cart by the User associated with the order
            var user = _context.Users.FirstOrDefault(u => u.Email == order.CustomerEmail);

            if (user != null)
            {
                // 1. Find the specific CartId for this user
                var cart = _context.ShopCarts
                    .FirstOrDefault(c => c.UserId == user.Id);

                if (cart != null)
                {
                    // 2. Remove all items belonging to THIS specific CartId
                    var itemsToRemove = _context.CartItems
                        .Where(ci => ci.ShopCartId == cart.Id)
                        .ToList();

                    if (itemsToRemove.Any())
                    {
                        _context.CartItems.RemoveRange(itemsToRemove);

                        // Optional: If you want to delete the cart itself too
                        // _context.ShopCarts.Remove(cart);
                    }
                }
            }

            _context.SaveChanges();
        }
    }
}
