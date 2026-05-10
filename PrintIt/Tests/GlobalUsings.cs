// Standard Test Frameworks
// Your Project Specifics (The most important part)
global using Data;       // So AppDbContext is always visible
global using Entities.Models;     // For ShopCart, CartItem, etc.
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.Data.Sqlite;
// Database & Core Framework
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using Services;   // So IOrderService/ICartService are always visible
global using Xunit;
global using WebProgram = Program;
