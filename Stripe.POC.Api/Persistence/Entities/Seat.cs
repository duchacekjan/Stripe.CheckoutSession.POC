using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Seat : Entity
{
    public string Row { get; set; } = string.Empty;
    public uint Number { get; set; }

    public long PriceId { get; set; }
    public long? OrderItemId { get; set; }
    public long PerformanceId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public Price Price { get; set; } = null!;
    public Performance Performance { get; set; } = null!;
}

public class SeatEntityConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.Row).HasMaxLength(50).IsRequired();
        builder.HasOne(o => o.Performance)
            .WithMany(p => p.Seats)
            .HasForeignKey(f => f.PerformanceId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete seats when performance is deleted
        
        builder.ToTable("Seats");
    }
}