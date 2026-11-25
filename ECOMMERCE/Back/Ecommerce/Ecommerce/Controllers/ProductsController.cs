using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(StoreDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? brand,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string sort = "featured")
    {
        var query = db.Products.AsNoTracking().Include(product => product.Category)
            .Include(product => product.Images).Include(product => product.Reviews).AsQueryable();

        if (!User.IsInRole("Administrator") && !User.IsInRole("StoreManager"))
            query = query.Where(product => product.Status == ProductStatus.Active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product => product.Name.Contains(term)
                || product.Description.Contains(term) || product.Brand.Contains(term));
        }
        if (categoryId.HasValue)
            query = query.Where(product => product.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(product => product.Brand == brand.Trim());
        if (minPrice.HasValue)
            query = query.Where(product => product.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(product => product.Price <= maxPrice.Value);

        query = sort.ToLowerInvariant() switch
        {
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "newest" => query.OrderByDescending(product => product.CreatedAtUtc),
            "name" => query.OrderBy(product => product.Name),
            _ => query.OrderByDescending(product => product.Reviews.Average(review => (double?)review.Rating) ?? 0)
        };

        var products = await query.ToListAsync();
        return Ok(products.Select(ToDto).ToArray());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await db.Products.AsNoTracking().Include(item => item.Category)
            .Include(item => item.Images).Include(item => item.Reviews)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (product is null || (product.Status == ProductStatus.Inactive
            && !User.IsInRole("Administrator") && !User.IsInRole("StoreManager")))
            return NotFound();
        return Ok(ToDto(product));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request)
    {
        var validation = Validate(request);
        if (validation is not null)
            return BadRequest(new { message = validation });
        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive))
            return BadRequest(new { message = "Choose an active category." });

        var product = new Product();
        Apply(product, request);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var created = await LoadProduct(product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(created!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductRequest request)
    {
        var validation = Validate(request);
        if (validation is not null)
            return BadRequest(new { message = validation });
        var product = await db.Products.Include(item => item.Images).FirstOrDefaultAsync(item => item.Id == id);
        if (product is null)
            return NotFound();
        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive))
            return BadRequest(new { message = "Choose an active category." });

        Apply(product, request);
        db.ProductImages.RemoveRange(product.Images);
        product.Images = request.Images.Where(url => !string.IsNullOrWhiteSpace(url))
            .Select((url, index) => new ProductImage { Url = url.Trim(), SortOrder = index }).ToList();
        await db.SaveChangesAsync();
        return Ok(ToDto((await LoadProduct(id))!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<IActionResult> Archive(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        product.Status = ProductStatus.Inactive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private Task<Product?> LoadProduct(int id) => db.Products.AsNoTracking()
        .Include(product => product.Category).Include(product => product.Images)
        .Include(product => product.Reviews).FirstOrDefaultAsync(product => product.Id == id);

    private static ProductDto ToDto(Product product) => new(
        product.Id, product.Name, product.Description, product.Brand, product.Price, product.EffectivePrice,
        product.DiscountPercent, product.CategoryId, product.Category.Name, product.Stock, product.MinimumStock,
        product.Status, product.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).ToArray(),
        product.Reviews.Count == 0 ? 0 : Math.Round(product.Reviews.Average(review => review.Rating), 1),
        product.Reviews.Count, product.CreatedAtUtc);

    private static string? Validate(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 180)
            return "Product name is required and must be 180 characters or fewer.";
        if (request.Price < 0 || request.DiscountPercent is < 0 or > 100)
            return "Price must be non-negative and discount must be between 0 and 100.";
        if (request.Stock < 0 || request.MinimumStock < 0)
            return "Stock values must be non-negative.";
        if (request.Images.Any(image => image.Length > 1000))
            return "Image URLs must be 1,000 characters or fewer.";
        return null;
    }

    private static void Apply(Product product, ProductRequest request)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Brand = request.Brand.Trim();
        product.Price = request.Price;
        product.DiscountPercent = request.DiscountPercent;
        product.DiscountStartsAtUtc = request.DiscountStartsAtUtc;
        product.DiscountEndsAtUtc = request.DiscountEndsAtUtc;
        product.CategoryId = request.CategoryId;
        product.Stock = request.Stock;
        product.MinimumStock = request.MinimumStock;
        product.Status = request.Status;
        if (product.Id == 0)
        {
            product.Images = request.Images.Where(url => !string.IsNullOrWhiteSpace(url))
                .Select((url, index) => new ProductImage { Url = url.Trim(), SortOrder = index }).ToList();
        }
    }
}
