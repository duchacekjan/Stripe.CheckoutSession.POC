using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Price : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PriceEntityConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();

        builder.HasMany<Seat>()
            .WithOne(o => o.Price)
            .HasForeignKey(f => f.PriceId)
            .OnDelete(DeleteBehavior.Restrict); // Assuming you don't want to delete prices when seats are deleted
        
        builder.ToTable("Prices");
    }
}