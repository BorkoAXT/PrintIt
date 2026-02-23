using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrintIt.Data;
using PrintIt.Models;
using Stripe;
using System.Security.Claims;
using System.Text.Json;

namespace PrintIt.Controllers
{
    public class OrderController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly PaymentsOptions _payments;
        private readonly IConfiguration _configuration;

        public OrderController(
            ApplicationDbContext context, IConfiguration configuration,
            IOptions<PaymentsOptions> payments) : base(context)
        {
            _context = context;
            _configuration = configuration;
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

        //public IActionResult PayEpay(int orderId, decimal amount)
        //{
        //    // 1. Prepare your data string
        //    string data = $"MIN={_payments.Epay.Cin}\nINVOICE={orderId}\nAMOUNT={amount.ToString("F2")}\nEXP_TIME={DateTime.Now.AddDays(1):dd.MM.yyyy}";

        //    // 2. Encode to Base64
        //    string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

        //    // 3. Generate Checksum
        //    using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_payments.Epay.Secret));
        //    string checksum = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(encoded))).Replace("-", "").ToLower();

        //    // 4. REDIRECT to the Mock Gate with the data in the URL
        //    return RedirectToAction("MockPaymentGate", new { encoded = encoded, checksum = checksum });
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(decimal amount, string email)
        //{
        //    try
        //    {
        //        var order = new Order
        //        {
        //            Amount = amount,
        //            CustomerEmail = email ?? "guest@example.com", // Fallback
        //            Currency = _payments.Currency ?? "EUR",
        //            Status = "Pending",
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        _context.Orders.Add(order);
        //        _context.SaveChanges();


        //        return RedirectToAction("PayEpay", new { orderId = order.Id, amount = order.Amount });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content($"Error: {ex.Message} inner: {ex.InnerException?.Message}");
        //    }
        //}

        //// This action simulates the external payment page
        //[HttpGet]
        //public IActionResult MockPaymentGate(string encoded, string checksum)
        //{
        //    if (string.IsNullOrEmpty(encoded))
        //    {
        //        return Content("Error: No encoded data received from the cart.");
        //    }

        //    ViewBag.Encoded = encoded;
        //    ViewBag.Checksum = checksum;

        //    return View();
        //}

        //[HttpPost]
        //public IActionResult SimulateCallback(string encoded)
        //{
        //    if (string.IsNullOrEmpty(encoded))
        //    {
        //        return BadRequest("Encoded data is missing.");
        //    }

        //    try
        //    {
        //        // Decode the Base64 string
        //        var decodedBytes = Convert.FromBase64String(encoded);
        //        var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);

        //        // Parse the lines to find the Invoice ID
        //        var lines = decodedText.Split('\n');
        //        var invoiceLine = lines.FirstOrDefault(l => l.StartsWith("INVOICE="));

        //        if (invoiceLine != null)
        //        {
        //            var orderIdStr = invoiceLine.Replace("INVOICE=", "").Trim();

        //            if (int.TryParse(orderIdStr, out int orderId))
        //            {
        //                // Logic for the order can go here later
        //                return Ok(new { message = "Callback processed successfully", invoiceId = orderId });
        //            }
        //        }

        //        return BadRequest("Error: Could not find a valid Invoice ID in the encoded data.");
        //    }
        //    catch (FormatException)
        //    {
        //        return BadRequest("Error: Invalid Base64 string.");
        //    }
        //}


        //[HttpPost]
        //public IActionResult EpayNotification()
        //{
        //    var encoded = Request.Form["encoded"];
        //    var checksum = Request.Form["checksum"];

        //    using var hmac = new HMACSHA1(
        //        Encoding.UTF8.GetBytes(_payments.Epay.Secret));

        //    var computed = BitConverter
        //        .ToString(hmac.ComputeHash(
        //            Encoding.UTF8.GetBytes(encoded)))
        //        .Replace("-", "")
        //        .ToLower();

        //    if (computed != checksum)
        //        return Content("ERR=Invalid checksum");

        //    var decoded = Encoding.UTF8.GetString(
        //        Convert.FromBase64String(encoded));


        //    var lines = decoded.Split('\n');

        //    foreach (var line in lines)
        //    {
        //        if (line.Contains("STATUS=PAID"))
        //        {
        //            var invoicePart = line.Split(':')[0];
        //            var orderId = int.Parse(
        //                invoicePart.Replace("INVOICE=", ""));

        //            MarkOrderAsPaid(orderId);
        //        }
        //    }

        //    return Content("OK");
        //}
        //public IActionResult PayMyPos(int orderId, decimal amount)
        //{
        //    var mypos = _payments.MyPos;

        //    var order = orderId.ToString();
        //    var amountStr = amount.ToString("F2");

        //    var dataToSign =
        //        $"{mypos.Sid}{order}{amountStr}{_payments.Currency}";

        //    var signature = SignMyPos(
        //        dataToSign,
        //        mypos.PrivateKey);

        //    ViewBag.Endpoint = mypos.Endpoint;
        //    ViewBag.Sid = mypos.Sid;
        //    ViewBag.Order = order;
        //    ViewBag.Amount = amountStr;
        //    ViewBag.Currency = _payments.Currency;
        //    ViewBag.Wallet = mypos.WalletNumber;
        //    ViewBag.KeyIndex = mypos.KeyIndex;
        //    ViewBag.Signature = signature;

        //    return View("MyPosRedirect");
        //}

        //private string SignMyPos(string data, string privateKeyPem)
        //{
        //    using var rsa = RSA.Create();
        //    rsa.ImportFromPem(privateKeyPem);

        //    var bytes = Encoding.UTF8.GetBytes(data);

        //    var signature = rsa.SignData(
        //        bytes,
        //        HashAlgorithmName.SHA256,
        //        RSASignaturePadding.Pkcs1);

        //    return Convert.ToBase64String(signature);
        //}

        //public IActionResult Success()
        //{
        //    return View();
        //}

        //public IActionResult Cancel()
        //{
        //    return View();
        //}

        //private void MarkOrderAsPaid(int orderId)
        //{
        //    var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
        //    if (order == null) return;

        //    order.Status = "Paid";
        //    order.PaidAt = DateTime.UtcNow;

        //    var user = _context.Users.FirstOrDefault(u => u.Email == order.CustomerEmail);

        //    if (user != null)
        //    {
        //        var cart = _context.ShopCarts
        //            .FirstOrDefault(c => c.UserId == user.Id);

        //        if (cart != null)
        //        {
        //            // 2. Remove all items belonging to THIS specific CartId
        //            var itemsToRemove = _context.CartItems
        //                .Where(ci => ci.ShopCartId == cart.Id)
        //                .ToList();

        //            if (itemsToRemove.Any())
        //            {
        //                _context.CartItems.RemoveRange(itemsToRemove);
        //            }
        //        }
        //    }

        //    _context.SaveChanges();
        //}

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            // 1. Get the current User ID
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Get the cart from the DATABASE (not Session)
            var cartItems = await _context.ShopCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Print)
                .Where(c => c.UserId == userId)
                .SelectMany(c => c.Items)
                .ToListAsync();

            // 3. Check if empty
            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("Cart", "Cart");
            }

            // 4. Calculate totals
            decimal subtotal = cartItems.Sum(item => (item.Print?.Price ?? 0) * (item.Quantity > 0 ? item.Quantity : 1));
            decimal shippingLimit = 50.00m;
            decimal shipping = (subtotal > shippingLimit || subtotal == 0) ? 0 : 5.00m;

            ViewBag.TotalAmount = subtotal + shipping;
            ViewBag.CartItems = cartItems;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent()
        {
            // 1. Get the User ID
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userId)) return BadRequest();

            // 2. Get cart from DATABASE
            var cartItems = await _context.ShopCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Print)
                .Where(c => c.UserId == userId)
                .SelectMany(c => c.Items)
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest(new { error = "Количката е празна." });

            // 3. Calculation
            decimal subtotal = cartItems.Sum(item => (item.Print?.Price ?? 0) * (item.Quantity > 0 ? item.Quantity : 1));
            decimal shipping = (subtotal > 50.00m || subtotal == 0) ? 0 : 5.00m;
            var amountInCents = (long)((subtotal + shipping) * 100);

            // 4. Stripe Logic
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "eur",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string> { { "UserId", userId.ToString() } }
            };

            var service = new PaymentIntentService();
            try
            {
                var intent = await service.CreateAsync(options);
                return Json(new { clientSecret = intent.ClientSecret });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.StripeError.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Success()
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                // Find the user's cart
                var cart = await _context.ShopCarts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null && cart.Items.Any())
                {
                    // Clear all items from the database for this user
                    _context.CartItems.RemoveRange(cart.Items);
                    await _context.SaveChangesAsync();
                }
            }

            return View();
        }
    }
}
