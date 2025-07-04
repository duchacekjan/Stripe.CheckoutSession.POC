using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Event : Entity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Performance> Performances { get; set; } = [];
}

public class EventEntityConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.HasMany(p => p.Performances)
            .WithOne(o=>o.Event)
            .HasForeignKey(f => f.EventId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete performances when event is deleted
        builder.ToTable("Events");
    }
}