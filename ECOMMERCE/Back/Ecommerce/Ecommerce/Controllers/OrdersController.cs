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
[Route("api/orders")]
public sealed class OrdersController(StoreDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll()
    {
        var query = db.Orders.AsNoTracking().Include(order => order.Items).AsQueryable();
        if (!CanManageOrders())
            query = query.Where(order => order.UserId == UserId());
        var orders = await query.OrderByDescending(order => order.CreatedAtUtc).ToArrayAsync();
        return Ok(orders.Select(ToDto).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (order is null || (!CanManageOrders() && order.UserId != UserId()))
            return NotFound();
        return Ok(ToDto(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CheckoutRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var cart = await db.Carts.Include(item => item.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.UserId == UserId());
        if (cart is null || cart.Items.Count == 0)
            return BadRequest(new { message = "Your cart is empty." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var cartItem in cart.Items)
        {
            if (cartItem.Product.Status != ProductStatus.Active || cartItem.Product.Stock < cartItem.Quantity)
                return Conflict(new { message = $"{cartItem.Product.Name} is no longer available in the requested quantity." });
        }

        var orderItems = cart.Items.Select(item => new StoreOrderItem
        {
            ProductId = item.ProductId,
            ProductName = item.Product.Name,
            Quantity = item.Quantity,
            UnitPrice = item.Product.EffectivePrice
        }).ToList();
        var subtotal = orderItems.Sum(item => item.UnitPrice * item.Quantity);
        var standardShipping = configuration.GetValue<decimal>("Store:StandardShipping", 5m);
        var expressShipping = configuration.GetValue<decimal>("Store:ExpressShipping", 15m);
        var freeShippingThreshold = configuration.GetValue<decimal>("Store:FreeShippingThreshold", 100m);
        var shippingAmount = request.DeliveryMethod == "Express" ? expressShipping
            : subtotal >= freeShippingThreshold ? 0 : standardShipping;
        var taxRate = configuration.GetValue<decimal>("Store:TaxRate", 0.08m);
        var taxAmount = decimal.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);
        var order = new StoreOrder
        {
            UserId = UserId(),
            Subtotal = subtotal,
            ShippingAmount = shippingAmount,
            TaxAmount = taxAmount,
            TotalAmount = subtotal + shippingAmount + taxAmount,
            ShippingName = request.ShippingName.Trim(),
            ShippingEmail = request.ShippingEmail.Trim(),
            ShippingAddress = request.ShippingAddress.Trim(),
            ShippingCity = request.ShippingCity.Trim(),
            ShippingPostalCode = request.ShippingPostalCode.Trim(),
            ShippingCountry = request.ShippingCountry.Trim(),
            PaymentMethod = request.PaymentMethod.Trim(),
            DeliveryMethod = request.DeliveryMethod.Trim(),
            Items = orderItems
        };

        foreach (var cartItem in cart.Items)
            cartItem.Product.Stock -= cartItem.Quantity;
        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Stock changed while the order was being placed. Refresh your basket and try again." });
        }
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToDto(order));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<OrderDto>> SetStatus(int id, OrderStatusRequest request)
    {
        if (!Enum.IsDefined(request.Status))
            return BadRequest(new { message = "Choose a valid order status." });
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id);
        if (order is null)
            return NotFound();
        order.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok(ToDto(order));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool CanManageOrders() => User.IsInRole("Administrator") || User.IsInRole("StoreManager");

    private static string? Validate(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingName) || string.IsNullOrWhiteSpace(request.ShippingEmail)
            || string.IsNullOrWhiteSpace(request.ShippingAddress) || string.IsNullOrWhiteSpace(request.ShippingCity)
            || string.IsNullOrWhiteSpace(request.ShippingCountry))
            return "Name, email, address, city, and country are required.";
        if (request.ShippingEmail.Length > 256 || request.ShippingAddress.Length > 300
            || request.ShippingCity.Length > 120 || request.ShippingPostalCode.Length > 32
            || request.ShippingCountry.Length > 100)
            return "One or more shipping fields are too long.";
        if (request.PaymentMethod != "Cash on delivery")
            return "Cash on delivery is the only configured payment method.";
        if (request.DeliveryMethod is not ("Standard" or "Express"))
            return "Choose standard or express delivery.";
        return null;
    }

    private static OrderDto ToDto(StoreOrder order) => new(
        order.Id, order.Status, order.Subtotal, order.ShippingAmount, order.TaxAmount, order.TotalAmount,
        order.ShippingName, order.ShippingAddress, order.ShippingCity, order.ShippingCountry,
        order.PaymentMethod, order.DeliveryMethod, order.CreatedAtUtc,
        order.Items.Select(item => new OrderItemDto(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToArray());
}
