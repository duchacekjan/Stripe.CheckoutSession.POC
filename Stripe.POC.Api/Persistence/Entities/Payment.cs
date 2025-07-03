using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Payment : Entity
{
    public string SessionId { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
}

public enum PaymentStatus
{
    Created,
    Succeeded,
    Failed
}

public class PaymentEntityConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(p => p.SessionId)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(p => p.PaymentIntentId)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(p => p.Status)
            .IsRequired();
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);
        builder.HasOne(o => o.Order)
            .WithMany(w => w.Payments)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete payments when order is deleted
        
        builder.ToTable("Payments");
    }
}