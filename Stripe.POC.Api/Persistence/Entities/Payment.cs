using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Payment : Entity
{
    private PaymentStatus _status = PaymentStatus.Created;
    public string SessionId { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }

    public PaymentStatus Status
    {
        get => _status;
        set => SetPaymentStatus(value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public ICollection<PaymentHistory> History { get; set; } = new List<PaymentHistory>();

    private void SetPaymentStatus(PaymentStatus newStatus)
    {
        if (newStatus == _status && newStatus == PaymentStatus.Created)
        {
            History.Add(new PaymentHistory
            {
                OldStatus = _status,
                NewStatus = newStatus,
                CreatedAt = DateTime.UtcNow
            });
            _status = newStatus;
            return;
        }

        if (_status == newStatus)
        {
            return;
        }

        History.Add(new PaymentHistory
        {
            OldStatus = _status,
            NewStatus = newStatus,
            CreatedAt = DateTime.UtcNow
        });
        _status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
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
        builder.HasIndex(i => i.SessionId).IsUnique();
        builder.Property(p => p.PaymentIntentId)
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