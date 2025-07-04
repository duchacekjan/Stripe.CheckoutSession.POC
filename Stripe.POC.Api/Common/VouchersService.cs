using Microsoft.EntityFrameworkCore;
using POC.Api.Features.Inventory.Seed;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Common;

public class VouchersService(AppDbContext dbContext)
{
    private const string VoucherPrefix = "VOUCHER-";

    public async Task BuyVoucherAsync(long orderId, decimal amount, CancellationToken ct)
    {
        var priceId = await EnsurePriceCreated(amount, ct);
        var orderItemId = await EnsureOrderItemCreated(orderId, priceId, ct);

        var seatNumber = await dbContext.Seats
            .Where(s => s.OrderItemId == orderItemId)
            .Where(s => s.PerformanceId == Seed.Voucher.Performances.First().Id && s.PriceId == priceId)
            .CountAsync(ct);
        var seat = new Seat
        {
            OrderItemId = orderItemId,
            Row = Guid.NewGuid().ToString(),
            Number = (uint)Math.Abs(seatNumber),
            PriceId = priceId,
            PerformanceId = Seed.Voucher.Performances.First().Id,
        };

        var voucher = new Voucher
        {
            Seat = seat,
            InitialAmount = amount
        };
        dbContext.Vouchers.Add(voucher);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<ValidationResult> ValidateVoucherAsync(Guid basketId, string code, CancellationToken ct)
    {
        var voucher = await dbContext.Vouchers
            .Where(v => v.Seat.Row == code)
            .FirstOrDefaultAsync(ct);
        if (voucher is null)
        {
            return ValidationResult.Invalid("Voucher not found");
        }

        if (voucher.RemainingAmount <= 0)
        {
            return ValidationResult.Valid(voucher.RemainingAmount);
        }

        var itemsTotal = await dbContext.OrderItems
            .Where(w => w.Order.BasketId == basketId)
            .Select(s => s.Seats.Sum(seat => seat.Price.Amount))
            .SumAsync(ct);
        var vouchersTotal = await dbContext.Orders
            .Where(w => w.BasketId == basketId)
            .SelectMany(s => s.Vouchers.Select(v => v.Amount))
            .SumAsync(ct);

        var totalPrice = itemsTotal - vouchersTotal;

        var discount = voucher.RemainingAmount > totalPrice
            ? totalPrice
            : voucher.RemainingAmount;

        return ValidationResult.Valid(discount);
    }

    public async Task RedeemVoucherAsync(Guid basketId, string code, CancellationToken ct)
    {
        // Logic to redeem the voucher
    }

    private async Task CreateVoucher(long orderId, long priceId, CancellationToken ct)
    {
    }

    private async Task<long> EnsureOrderItemCreated(long orderId, long priceId, CancellationToken cancellationToken)
    {
        var orderItem = await dbContext.OrderItems
            .Where(w => w.OrderId == orderId)
            .Where(w => w.Seats.Any(s => s.PerformanceId == Seed.Voucher.Performances.First().Id && s.PriceId == priceId))
            .FirstOrDefaultAsync(cancellationToken);

        if (orderItem is not null)
        {
            return orderItem.Id;
        }

        orderItem = new OrderItem
        {
            OrderId = orderId,
            Seats = []
        };
        var entity = dbContext.OrderItems.Add(orderItem).Entity;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    private async Task<long> EnsurePriceCreated(decimal price, CancellationToken cancellationToken)
    {
        var existingPrice = await dbContext.Prices
            .Where(p => p.Name.StartsWith(VoucherPrefix))
            .Where(p => p.Amount == price)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPrice is not null)
        {
            return existingPrice.Id;
        }

        var newPrice = new Price { Amount = price, Name = $"{VoucherPrefix}{price:N3}" };
        var entity = dbContext.Prices.Add(newPrice).Entity;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public class ValidationResult
    {
        private ValidationResult()
        {
        }

        public bool IsValid => Discount.HasValue;
        public decimal? Discount { get; init; }

        public string? ErrorMessage { get; init; }

        public static ValidationResult Valid(decimal? discount = null)
            => new() { Discount = discount };

        public static ValidationResult Invalid(string errorMessage)
            => new() { ErrorMessage = errorMessage };
    }
}