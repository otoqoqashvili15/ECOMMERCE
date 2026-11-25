using System.Security.Claims;
using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Route("api/products/{productId:int}/reviews")]
public sealed class ReviewsController(StoreDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> Get(int productId) => Ok(
        await db.ProductReviews.AsNoTracking().Where(review => review.ProductId == productId)
            .OrderByDescending(review => review.CreatedAtUtc)
            .Select(review => new ReviewDto(review.Id, review.User.DisplayName, review.Rating, review.Comment, review.CreatedAtUtc))
            .ToArrayAsync());

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Upsert(int productId, ReviewRequest request)
    {
        if (request.Rating is < 1 or > 5 || request.Comment.Length > 2000)
            return BadRequest(new { message = "Rating must be 1 to 5 and review text must be at most 2,000 characters." });
        if (!await db.Products.AnyAsync(product => product.Id == productId && product.Status == ProductStatus.Active))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var review = await db.ProductReviews.FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId);
        if (review is null)
        {
            review = new ProductReview { ProductId = productId, UserId = userId };
            db.ProductReviews.Add(review);
        }
        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.CreatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var user = await db.Users.FindAsync(userId);
        return Ok(new ReviewDto(review.Id, user?.DisplayName ?? "Customer", review.Rating, review.Comment, review.CreatedAtUtc));
    }
}
