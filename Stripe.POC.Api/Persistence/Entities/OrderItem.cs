using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace POC.Api.Persistence.Entities;

[Table("OrderItems")]
public class OrderItem : Entity
{
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = [];
}

public class OrderItemsEntityConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasMany(o => o.Seats)
            .WithOne(s => s.OrderItem)
            .HasForeignKey(f => f.OrderItemId);
    }
}