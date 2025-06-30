using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

[Table("Orders")]
public class Order : Entity
{
    public Guid BasketId { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public CheckoutSession CheckoutSession { get; set; } = null!;
}

public enum OrderStatus
{
    Created,
    Paid,
    Cancelled
}

public class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.BasketId).IsRequired();
        builder.HasMany(p => p.OrderItems)
            .WithOne(o => o.Order)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete order items when order is deleted
        builder.HasIndex(i => i.BasketId).IsUnique();
    }
}