using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

[Table("Performances")]
public class Performance : Entity
{
    public DateTime PerformanceDate { get; set; }
    public uint DurationMinutes { get; set; }

    public long EventId { get; set; }
    public ICollection<Seat> Seats { get; set; } = [];
    public Event Event { get; set; } = null!;
}

public class PerformanceEntityConfiguration : IEntityTypeConfiguration<Performance>
{
    public void Configure(EntityTypeBuilder<Performance> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.PerformanceDate).IsRequired();
        builder.Property(p => p.DurationMinutes).IsRequired();
    }
}