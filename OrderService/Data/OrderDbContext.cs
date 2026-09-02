using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.OrderId);

            entity.Property(o => o.OrderId)
                  .HasColumnName("order_id")
                  .HasDefaultValueSql("NEWID()");

            entity.Property(o => o.UserId)
                  .HasColumnName("user_id")
                  .IsRequired();

            entity.Property(o => o.OrderStatus)
                  .HasColumnName("order_status")
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(o => o.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.ToTable(t => t.HasCheckConstraint(
                "chk_order_status",
                "[order_status] IN ('CREATED','CONFIRMED','CANCELLED')"));

            entity.HasIndex(o => o.UserId).HasDatabaseName("idx_orders_user_id");

            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(i => i.OrderItemId);

            entity.Property(i => i.OrderItemId)
                  .HasColumnName("order_item_id")
                  .HasDefaultValueSql("NEWID()");

            entity.Property(i => i.OrderId)
                  .HasColumnName("order_id")
                  .IsRequired();

            entity.Property(i => i.ProductId)
                  .HasColumnName("product_id")
                  .IsRequired();

            entity.Property(i => i.Quantity)
                  .HasColumnName("quantity")
                  .IsRequired();

            entity.ToTable(t => t.HasCheckConstraint("chk_order_item_quantity", "[quantity] > 0"));

            entity.HasIndex(i => i.ProductId).HasDatabaseName("idx_order_items_product_id");
        });
    }
}
