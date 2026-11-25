using Ecommerce.Entities;

namespace Ecommerce.Contracts;

public sealed record UserDto(string Id, string DisplayName, string Email, IReadOnlyList<string> Roles);
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
public sealed record RegisterRequest(string DisplayName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);

public sealed record ProductDto(
    int Id,
    string Name,
    string Description,
    string Brand,
    decimal Price,
    decimal EffectivePrice,
    decimal DiscountPercent,
    int CategoryId,
    string CategoryName,
    int Stock,
    int MinimumStock,
    ProductStatus Status,
    IReadOnlyList<string> Images,
    double Rating,
    int ReviewCount,
    DateTime CreatedAtUtc);

public sealed record ProductRequest
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal DiscountPercent { get; init; }
    public DateTime? DiscountStartsAtUtc { get; init; }
    public DateTime? DiscountEndsAtUtc { get; init; }
    public int CategoryId { get; init; }
    public int Stock { get; init; }
    public int MinimumStock { get; init; }
    public ProductStatus Status { get; init; } = ProductStatus.Active;
    public List<string> Images { get; init; } = [];
}

public sealed record CategoryDto(int Id, string Name, string Description, string ImageUrl, int? ParentId, int ProductCount);
public sealed record CategoryRequest(string Name, string Description, string ImageUrl, int? ParentId);
public sealed record ReviewRequest(int Rating, string Comment);
public sealed record ReviewDto(int Id, string DisplayName, int Rating, string Comment, DateTime CreatedAtUtc);

public sealed record CartItemDto(
    int Id,
    int ProductId,
    string Name,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity,
    int Stock,
    decimal LineTotal);
public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal Subtotal);
public sealed record CartItemRequest(int ProductId, int Quantity);
public sealed record CartQuantityRequest(int Quantity);

public sealed record CheckoutRequest(
    string ShippingName,
    string ShippingEmail,
    string ShippingAddress,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    string PaymentMethod,
    string DeliveryMethod);
public sealed record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
public sealed record OrderDto(
    int Id,
    OrderStatus Status,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string ShippingName,
    string ShippingAddress,
    string ShippingCity,
    string ShippingCountry,
    string PaymentMethod,
    string DeliveryMethod,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items);
public sealed record OrderStatusRequest(OrderStatus Status);
public sealed record ProfileRequest(string DisplayName, string Email);
public sealed record AdminSummaryDto(int ProductCount, int OrderCount, int LowStockCount, decimal SalesTotal);
public sealed record CheckoutSettingsDto(decimal TaxRate, decimal StandardShipping, decimal ExpressShipping, decimal FreeShippingThreshold);
