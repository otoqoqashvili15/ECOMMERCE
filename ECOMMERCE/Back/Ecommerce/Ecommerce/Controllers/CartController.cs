using System.Security.Claims;
using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public sealed class CartController(StoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> Get()
    {
        var cart = await GetOrCreateCart();
        return Ok(ToDto(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(CartItemRequest request)
    {
        if (request.Quantity is < 1 or > 100)
            return BadRequest(new { message = "Quantity must be between 1 and 100." });
        var product = await db.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId && item.Status == ProductStatus.Active);
        if (product is null)
            return NotFound(new { message = "Product was not found." });
        if (product.Stock < request.Quantity)
            return Conflict(new { message = "There is not enough stock for this quantity." });

        var cart = await GetOrCreateCart();
        var item = cart.Items.FirstOrDefault(existing => existing.ProductId == request.ProductId);
        if (item is null)
            cart.Items.Add(new CartItem { ProductId = product.Id, Product = product, Quantity = request.Quantity });
        else if (item.Quantity + request.Quantity > product.Stock)
            return Conflict(new { message = "There is not enough stock for this quantity." });
        else
            item.Quantity += request.Quantity;

        await db.SaveChangesAsync();
        return Ok(ToDto(await LoadCart()));
    }

    [HttpPut("items/{id:int}/quantity")]
    public async Task<ActionResult<CartDto>> SetQuantity(int id, CartQuantityRequest request)
    {
        if (request.Quantity is < 1 or > 100)
            return BadRequest(new { message = "Quantity must be between 1 and 100." });
        var item = await db.CartItems.Include(cartItem => cartItem.Product).Include(cartItem => cartItem.Cart)
            .FirstOrDefaultAsync(cartItem => cartItem.Id == id && cartItem.Cart.UserId == UserId());
        if (item is null)
            return NotFound();
        if (request.Quantity > item.Product.Stock)
            return Conflict(new { message = "There is not enough stock for this quantity." });
        item.Quantity = request.Quantity;
        await db.SaveChangesAsync();
        return Ok(ToDto(await LoadCart()));
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int id)
    {
        var item = await db.CartItems.Include(cartItem => cartItem.Cart)
            .FirstOrDefaultAsync(cartItem => cartItem.Id == id && cartItem.Cart.UserId == UserId());
        if (item is null)
            return NotFound();
        db.CartItems.Remove(item);
        await db.SaveChangesAsync();
        return Ok(ToDto(await LoadCart()));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<Cart> GetOrCreateCart()
    {
        var cart = await db.Carts.Include(item => item.Items).ThenInclude(item => item.Product)
            .ThenInclude(product => product.Images)
            .FirstOrDefaultAsync(item => item.UserId == UserId());
        if (cart is not null)
            return cart;
        cart = new Cart { UserId = UserId() };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return cart;
    }

    private async Task<Cart> LoadCart()
    {
        await GetOrCreateCart();
        return (await db.Carts.AsNoTracking().Include(item => item.Items).ThenInclude(item => item.Product)
            .ThenInclude(product => product.Images).FirstAsync(item => item.UserId == UserId()));
    }

    private static CartDto ToDto(Cart cart)
    {
        var items = cart.Items.Select(item => new CartItemDto(
            item.Id, item.ProductId, item.Product.Name, item.Product.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.Url ?? "",
            item.Product.EffectivePrice, item.Quantity, item.Product.Stock,
            item.Product.EffectivePrice * item.Quantity)).ToArray();
        return new CartDto(items, items.Sum(item => item.LineTotal));
    }
}
