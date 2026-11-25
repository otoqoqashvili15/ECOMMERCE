using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(StoreDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll() => Ok(
        await db.Categories.AsNoTracking().Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name, category.Description,
                category.ImageUrl, category.ParentId,
                category.Products.Count(product => product.Status == ProductStatus.Active)))
            .ToArrayAsync());

    [HttpPost]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });
        if (request.ParentId.HasValue && !await db.Categories.AnyAsync(category => category.Id == request.ParentId && category.IsActive))
            return BadRequest(new { message = "Parent category was not found." });
        var category = new Category();
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = category.Id },
            new CategoryDto(category.Id, category.Name, category.Description, category.ImageUrl, category.ParentId, 0));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });
        if (request.ParentId == id)
            return BadRequest(new { message = "A category cannot be its own parent." });
        if (request.ParentId.HasValue && !await db.Categories.AnyAsync(item => item.Id == request.ParentId && item.IsActive))
            return BadRequest(new { message = "Parent category was not found." });
        var category = await db.Categories.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
        if (category is null)
            return NotFound();
        Apply(category, request);
        await db.SaveChangesAsync();
        return Ok(new CategoryDto(category.Id, category.Name, category.Description, category.ImageUrl,
            category.ParentId, await db.Products.CountAsync(product => product.CategoryId == id && product.Status == ProductStatus.Active)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<IActionResult> Archive(int id)
    {
        var category = await db.Categories.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
        if (category is null)
            return NotFound();
        if (await db.Products.AnyAsync(product => product.CategoryId == id && product.Status == ProductStatus.Active)
            || await db.Categories.AnyAsync(child => child.ParentId == id && child.IsActive))
            return Conflict(new { message = "Move or archive this category's products and subcategories first." });
        category.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private string? Validate(CategoryRequest request) => string.IsNullOrWhiteSpace(request.Name)
        || request.Name.Length > 120
            ? "Category name is required and must be 120 characters or fewer."
            : request.Description.Length > 1000 || request.ImageUrl.Length > 1000
                ? "Category description or image URL is too long."
                : null;

    private static void Apply(Category category, CategoryRequest request)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        category.ImageUrl = request.ImageUrl.Trim();
        category.ParentId = request.ParentId;
    }
}
