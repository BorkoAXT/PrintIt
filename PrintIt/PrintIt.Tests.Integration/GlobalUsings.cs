// Standard Test Frameworks
global using Xunit;
global using Microsoft.AspNetCore.Mvc.Testing;

// Database & Core Framework
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Data.Sqlite;

// Your Project Specifics (The most important part)
global using PrintIt.Data;       // So AppDbContext is always visible
global using PrintIt.Services;   // So IOrderService/ICartService are always visible
global using PrintIt.Models;     // For ShopCart, CartItem, etc.

// The "Program" Fix
global using WebProgram = PrintIt.Program;