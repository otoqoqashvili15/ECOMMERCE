using Ecommerce.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Data;

public static class StoreBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Administrator", "StoreManager", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Store administrator"
                };
                var created = await userManager.CreateAsync(admin, adminPassword);
                if (!created.Succeeded)
                    throw new InvalidOperationException(string.Join(" ", created.Errors.Select(error => error.Description)));
            }
            if (!await userManager.IsInRoleAsync(admin, "Administrator"))
                await userManager.AddToRoleAsync(admin, "Administrator");
        }

        if (await db.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Name = "Audio", Description = "Headphones, speakers, and everyday listening." },
            new() { Name = "Home", Description = "Thoughtful pieces for a calmer home." },
            new() { Name = "Accessories", Description = "Useful details for work and travel." },
            new() { Name = "Wearables", Description = "Comfortable technology for every day." }
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var products = new List<Product>
        {
            ProductSeed("Studio Wireless Headphones", "Balanced sound, soft memory-foam cushions, and up to 32 hours of listening.", "Northstar", 189, 15, 42, 5, categories[0], "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=85"),
            ProductSeed("Arc Portable Speaker", "A compact room-filling speaker with a woven finish and all-day battery.", "Northstar", 129, 0, 28, 4, categories[0], "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=900&q=85"),
            ProductSeed("Linen Table Lamp", "Warm dimmable light with a natural linen shade and solid oak base.", "Form & Field", 96, 10, 17, 3, categories[1], "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=900&q=85"),
            ProductSeed("Everyday Carry Tote", "A durable recycled-canvas tote with a padded sleeve and interior organizer.", "Common Goods", 58, 0, 64, 8, categories[2], "https://images.unsplash.com/photo-1590874103328-eac38a683ce7?auto=format&fit=crop&w=900&q=85"),
            ProductSeed("Minimal Watch", "A clean stainless-steel profile, sapphire glass, and a comfortable quick-release strap.", "Aster", 215, 0, 11, 3, categories[3], "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=900&q=85"),
            ProductSeed("Ceramic Pour-over Set", "A small-batch ceramic dripper and matching cup for a slower morning ritual.", "Form & Field", 72, 0, 23, 4, categories[1], "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=900&q=85")
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private static Product ProductSeed(string name, string description, string brand, decimal price,
        decimal discount, int stock, int minimumStock, Category category, string imageUrl)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Brand = brand,
            Price = price,
            DiscountPercent = discount,
            Stock = stock,
            MinimumStock = minimumStock,
            Category = category,
            Status = ProductStatus.Active
        };
        product.Images.Add(new ProductImage { Url = imageUrl, SortOrder = 0 });
        return product;
    }
}
