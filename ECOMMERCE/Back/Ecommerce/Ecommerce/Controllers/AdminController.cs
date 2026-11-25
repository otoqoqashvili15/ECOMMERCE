using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,StoreManager")]
[Route("api/admin")]
public sealed class AdminController(StoreDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminSummaryDto>> GetSummary()
    {
        var activeProducts = db.Products.Where(product => product.Status == ProductStatus.Active);
        var salesTotal = await db.Orders.Where(order => order.Status == OrderStatus.Paid
                || order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        return Ok(new AdminSummaryDto(
            await activeProducts.CountAsync(),
            await db.Orders.CountAsync(),
            await activeProducts.CountAsync(product => product.Stock <= product.MinimumStock),
            salesTotal));
    }

    [HttpGet("inventory/low-stock")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetLowStock()
    {
        var products = await db.Products.AsNoTracking().Where(product => product.Status == ProductStatus.Active
                && product.Stock <= product.MinimumStock)
            .Include(product => product.Category).Include(product => product.Images).Include(product => product.Reviews)
            .OrderBy(product => product.Stock).ToArrayAsync();
        return Ok(products.Select(product => new ProductDto(
            product.Id, product.Name, product.Description, product.Brand, product.Price, product.EffectivePrice,
            product.DiscountPercent, product.CategoryId, product.Category.Name, product.Stock,
            product.MinimumStock, product.Status, product.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).ToArray(),
            product.Reviews.Count == 0 ? 0 : Math.Round(product.Reviews.Average(review => review.Rating), 1),
            product.Reviews.Count, product.CreatedAtUtc)).ToArray());
    }
}
