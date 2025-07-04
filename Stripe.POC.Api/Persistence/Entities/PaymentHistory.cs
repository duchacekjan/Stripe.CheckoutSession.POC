using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class PaymentHistory : Entity
{
    public long PaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public PaymentStatus OldStatus { get; set; }
    public PaymentStatus NewStatus { get; set; }
    public Payment Payment { get; set; } = null!;
}

public class PaymentHistoryEntityConfiguration : IEntityTypeConfiguration<PaymentHistory>
{
    public void Configure(EntityTypeBuilder<PaymentHistory> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        builder.Property(p => p.OldStatus)
            .IsRequired();
        builder.Property(p => p.NewStatus)
            .IsRequired();
        builder.HasOne(o => o.Payment)
            .WithMany(w => w.History)
            .HasForeignKey(f => f.PaymentId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete history when payment is deleted

        builder.ToTable("PaymentHistories");
    }
}