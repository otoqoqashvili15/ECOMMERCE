using Ecommerce.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Data;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<StoreOrder> Orders => Set<StoreOrder>();
    public DbSet<StoreOrderItem> OrderItems => Set<StoreOrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().Property(user => user.DisplayName).HasMaxLength(120);

        builder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.Property(category => category.ImageUrl).HasMaxLength(1000);
            entity.HasOne(category => category.Parent)
                .WithMany(category => category.Children)
                .HasForeignKey(category => category.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name).HasMaxLength(180).IsRequired();
            entity.Property(product => product.Brand).HasMaxLength(120);
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
            entity.Property(product => product.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(product => product.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(product => product.RowVersion).IsRowVersion();
            entity.HasIndex(product => product.Name);
            entity.HasIndex(product => product.Brand);
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.Property(image => image.Url).HasMaxLength(1000).IsRequired();
            entity.HasOne(image => image.Product).WithMany(product => product.Images)
                .HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductReview>(entity =>
        {
            entity.Property(review => review.Comment).HasMaxLength(2000);
            entity.HasIndex(review => new { review.ProductId, review.UserId }).IsUnique();
            entity.HasOne(review => review.Product).WithMany(product => product.Reviews)
                .HasForeignKey(review => review.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(review => review.User).WithMany().HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cart>().HasIndex(cart => cart.UserId).IsUnique();
        builder.Entity<Cart>().HasOne(cart => cart.User).WithMany().HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CartItem>().HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
        builder.Entity<CartItem>().HasOne(item => item.Product).WithMany()
            .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CartItem>().HasOne(item => item.Cart).WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WishlistItem>().HasIndex(item => new { item.UserId, item.ProductId }).IsUnique();
        builder.Entity<WishlistItem>().HasOne(item => item.User).WithMany()
            .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WishlistItem>().HasOne(item => item.Product).WithMany()
            .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StoreOrder>(entity =>
        {
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(order => order.Subtotal).HasColumnType("decimal(18,2)");
            entity.Property(order => order.ShippingAmount).HasColumnType("decimal(18,2)");
            entity.Property(order => order.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(order => order.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(order => order.ShippingName).HasMaxLength(180);
            entity.Property(order => order.ShippingEmail).HasMaxLength(256);
            entity.Property(order => order.ShippingAddress).HasMaxLength(300);
            entity.Property(order => order.ShippingCity).HasMaxLength(120);
            entity.Property(order => order.ShippingPostalCode).HasMaxLength(32);
            entity.Property(order => order.ShippingCountry).HasMaxLength(100);
            entity.Property(order => order.PaymentMethod).HasMaxLength(80);
            entity.Property(order => order.DeliveryMethod).HasMaxLength(80);
            entity.HasOne(order => order.User).WithMany().HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StoreOrderItem>(entity =>
        {
            entity.Property(item => item.ProductName).HasMaxLength(180);
            entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(item => item.Order).WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product).WithMany()
                .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
