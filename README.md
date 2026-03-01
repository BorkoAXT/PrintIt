# PrintIt

PrintIt is a .NET 9 web application for managing and selling printable items (e.g., 3D prints or custom products).  
It uses ASP.NET Core with Razor Pages and MVC, Entity Framework Core, and ASP.NET Core Identity for authentication and authorization.

---

## Features

- **Product catalog**
  - `Print` entities with name, description, material, weight, price, type, colors, and image path.
  - Categorization by `PrintType` and filtering by material, price range, and search term.
  - Recently added items ordered by `AddedOn`.

- **Authentication & authorization**
  - ASP.NET Core Identity with `User` (GUID primary key).
  - Built-in UI via Identity Razor Pages.
  - Google external login (OAuth 2).
  - Seeded roles: `Admin` and `User`.
  - Seeded admin user: `admin@gmail.com` / `Admin123` (development default, should be changed in production).

- **Shopping cart & orders**
  - Server-side cart tied to the authenticated user.
  - Checkout flow handled by `OrderController`.
  - Order total consists of subtotal + dynamically calculated shipping.
  - Cart is cleared after successful payment (`OrderService.ClearCartAsync`).

- **Payments**
  - `IPaymentService` abstraction with `PaymentService` implementation.
  - `OrderController.CreatePaymentIntent` creates a payment intent (e.g., Stripe) and returns `clientSecret` to the client.
  - Robust logging and error handling around payment creation.

- **Wishlist**
  - Per-user wishlist stored in `UserWishlistItem`.
  - Toggle favorite/unfavorite for a given `Print`.
  - Wishlist automatically cached per user for faster access.

- **Caching**
  - In-memory caching via `IMemoryCache`.
  - Global cache for catalog data in `PrintService`.
  - Per-user cache for wishlist data in `WishlistService`.
  - Cache invalidation on CRUD operations to keep data fresh.

---

## Architecture

- **Framework & Runtime**
  - .NET 9
  - ASP.NET Core (Razor Pages + MVC controllers & views)
  - Entity Framework Core with SQL Server

- **Entry point**
  - `Program.cs`
    - Configures:
      - `ApplicationDbContext` with `DefaultConnection`.
      - ASP.NET Core Identity (`User`, `IdentityRole<Guid>`).
      - Google Authentication.
      - Razor Pages and MVC.
      - Cookie authentication, session, caching.
      - DI registrations for application services.
    - Sets up middleware pipeline:
      - HTTPS redirection, static files, routing, session, authentication, authorization.
      - Development: `UseMigrationsEndPoint`.
      - Production: `UseExceptionHandler`, `UseHsts`.
    - Maps routes:
      - Default MVC route: `{controller=Home}/{action=Index}/{id?}`
      - Razor Pages via `MapRazorPages()`.
    - Seeds roles (`Admin`, `User`) and the default admin account.

- **Core domain model**
  - `Print`
    - `Id` (Guid, private set, generated in constructor)
    - `AddedOn` (UTC timestamp)
    - `Name`, `Description`
    - `MaterialType`, `PrintType` (enums)
    - `Weight` (double), `Price` (decimal)
    - `FilePath` (optional image/file URL)
    - `PrintColors` (list of `PrintColor` values)
    - Parameterless constructor for EF Core and a rich constructor for domain creation.

- **Services**

  ### `PrintService` (`IPrintService`)
  - Responsibilities:
    - Read/write operations for `Print` catalog.
    - Querying by id, type, or custom search filters.
    - Managing cache for catalog data.
  - Key methods:
    - `Task<List<Print>> GetAllAsync()`
    - `Task<Print?> GetByIdAsync(Guid id)`
    - `Task<List<Print>> GetByCategoryAsync(PrintType type)`
    - `Task<List<Print>> SearchAsync(StoreSearchViewModel filter)`
    - `Task AddAsync(Print print)`
    - `Task UpdateAsync(Print print)`
    - `Task DeleteAsync(Print print)`
    - `void ClearCache(Guid? id = null)`
  - Caching:
    - `Prints:All` – list of all prints.
    - `Prints:{id}` – single print.
    - `Prints:Category:{type}` – list by category.
    - Global cache lifetime: 30 minutes.
    - Cache cleared or partially invalidated after any write operation.

  ### `WishlistService` (`IWishlistService`)
  - Responsibilities:
    - Manage user wishlists and favorite items.
    - Keep frequently accessed wishlist data cached.
  - Key methods:
    - `Task<bool> IsFavoriteAsync(Guid userId, Guid printId)`
    - `Task<bool> ToggleAsync(Guid userId, Guid printId)` – returns new state (true = favorited).
    - `Task<List<UserWishlistItem>> GetItemsByPrintIdAsync(Guid printId)`
    - `Task<List<UserWishlistItem>> GetWishlistForUserAsync(Guid userId)`
    - `Task RemoveAsync(UserWishlistItem item)`
    - `Task RemoveAllByPrintIdAsync(Guid printId)` – removes all wishlist entries for a deleted print.
    - `void ClearCache(Guid userId)`
  - Caching:
    - `Wishlist:{userId}` – full wishlist with `Print` included.
    - Cache lifetime per user: 60 seconds.

  ### Other registered services
  - `ICartService` / `CartService`
  - `IFileService` / `FileService`
  - `IOrderService` / `OrderService`
  - `IPaymentService` / `PaymentService`  
  (Implement the business logic for cart operations, file handling, order management, and payments.)

- **Controllers**

  ### `OrderController`
  - Actions:
    - `Index` (GET)
      - Reads current user id from `ClaimTypes.NameIdentifier`.
      - Exposes it via `ViewData["UserId"]` for the view.
    - `Checkout` (GET)
      - Requires authenticated user; redirects to `/Identity/Account/Login` if not.
      - Loads cart items with `_orderService.GetCartItemsAsync(userId)`.
      - If cart is empty, redirects to `CartController.Cart`.
      - Calculates:
        - `subtotal` via `_orderService.CalculateCartTotalAsync(userId)`.
        - `shipping` via `_orderService.CalculateShippingAsync(subtotal)`.
      - Sets `ViewBag.TotalAmount` and `ViewBag.CartItems`.
    - `CreatePaymentIntent` (POST)
      - Requires authenticated user; otherwise returns `BadRequest`.
      - Validates that the cart is not empty.
      - Computes amount in cents (subtotal + shipping).
      - Calls `_paymentService.CreatePaymentIntentAsync(userId, amountInCents)`.
      - Returns JSON `{ clientSecret }` to the client.
    - `Success` (GET)
      - On successful payment, clears cart via `_orderService.ClearCartAsync(userId)`.
      - Displays success view.
  - Helper:
    - `TryGetUserId(out Guid userId)` – parses the user id from claims, returns `false` if not authenticated.

---

## Views & UI

- Razor Pages for:
  - Identity (login, register, manage account).
- MVC Views for:
  - Catalog browsing and details.
  - Cart, order, and checkout (`Views/Order/Index.cshtml`, `Views/Order/Checkout.cshtml`, etc.).
- Layouts and partials compose the shared UI around navigation, authentication, and cart/wishlist access.

---

## Configuration

- **Database**
  - Connection string `DefaultConnection` (SQL Server) is required in configuration.
- **Authentication**
  - Google sign-in requires:
    - `Authentication:Google:ClientId`
    - `Authentication:Google:ClientSecret`
- **Cookies & Session**
  - SameSite = Lax, HttpOnly cookies, secure policy set based on request.
  - Session timeout: 30 minutes.

---

## Running the Application

1. Ensure SQL Server is available and set `DefaultConnection` in `appsettings.json` / user secrets.
2. Configure Google OAuth credentials if using external login.
3. Apply EF Core migrations (`dotnet ef database update`).
4. Run the application:
    - `dotnet run` or via Visual Studio.
5. Sign in with the seeded admin user (`admin@gmail.com` / `Admin123`) and change the password immediately in non-development environments.

---

## Technologies Used

- .NET 9
- ASP.NET Core (Razor Pages + MVC)
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity
- IMemoryCache
- Google Authentication (OAuth 2)
- Dependency Injection (built-in to ASP.NET Core)
- Logging via `ILogger<>`