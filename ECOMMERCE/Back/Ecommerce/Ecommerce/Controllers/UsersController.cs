using System.Security.Claims;
using Ecommerce.Contracts;
using Ecommerce.Data;
using Ecommerce.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    StoreDbContext db,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrator,StoreManager")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll()
    {
        var allUsers = await users.Users.OrderBy(user => user.DisplayName).ToArrayAsync();
        var result = new List<UserDto>(allUsers.Length);
        foreach (var user in allUsers)
            result.Add(await ToDto(user));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, ProfileRequest request)
    {
        if (id != UserId() && !CanManageUsers())
            return Forbid();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 120
            || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "A display name and email are required." });
        var user = await users.FindByIdAsync(id);
        if (user is null)
            return NotFound();
        user.DisplayName = request.DisplayName.Trim();
        var emailResult = await users.SetEmailAsync(user, request.Email.Trim());
        if (!emailResult.Succeeded)
            return BadRequest(new { message = string.Join(" ", emailResult.Errors.Select(error => error.Description)) });
        var nameResult = await users.SetUserNameAsync(user, request.Email.Trim());
        if (!nameResult.Succeeded)
            return BadRequest(new { message = string.Join(" ", nameResult.Errors.Select(error => error.Description)) });
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(error => error.Description)) });
        return Ok(await ToDto(user));
    }

    [HttpGet("{id}/orders")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(string id)
    {
        if (id != UserId() && !CanManageUsers())
            return Forbid();
        var orders = await db.Orders.AsNoTracking().Include(order => order.Items)
            .Where(order => order.UserId == id).OrderByDescending(order => order.CreatedAtUtc).ToArrayAsync();
        return Ok(orders.Select(order => new OrderDto(
            order.Id, order.Status, order.Subtotal, order.ShippingAmount, order.TaxAmount, order.TotalAmount,
            order.ShippingName, order.ShippingAddress, order.ShippingCity, order.ShippingCountry,
            order.PaymentMethod, order.DeliveryMethod, order.CreatedAtUtc,
            order.Items.Select(item => new OrderItemDto(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToArray())).ToArray());
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool CanManageUsers() => User.IsInRole("Administrator") || User.IsInRole("StoreManager");
    private async Task<UserDto> ToDto(ApplicationUser user) =>
        new(user.Id, user.DisplayName, user.Email ?? string.Empty, (await users.GetRolesAsync(user)).ToArray());
}
