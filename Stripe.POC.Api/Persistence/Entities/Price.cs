using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace POC.Api.Persistence.Entities;

[Table("Prices")]
public class Price : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PriceEntityConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Price> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();

        builder.HasMany<Seat>()
            .WithOne(o => o.Price)
            .HasForeignKey(f => f.PriceId)
            .OnDelete(DeleteBehavior.Restrict); // Assuming you don't want to delete prices when seats are deleted
    }
}