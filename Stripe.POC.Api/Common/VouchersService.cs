using System.Diagnostics.CodeAnalysis;
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
        var result = await ValidateVoucherInternalAsync(basketId, code, ct);
        return result.Result;
    }

    public async Task RedeemVoucherAsync(Guid basketId, string code, CancellationToken ct)
    {
        var result = await ValidateVoucherInternalAsync(basketId, code, ct);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(result.Result.ErrorMessage ?? "Invalid voucher");
        }

        var voucher = await dbContext.Vouchers
            .Include(i => i.History)
            .Where(v => v.Id == result.VoucherId)
            .FirstAsync(ct);
        voucher.RedeemAmount(result.OrderId.Value, result.Result.Discount!.Value);
        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<ValidationResultInternal> ValidateVoucherInternalAsync(Guid basketId, string code, CancellationToken ct)
    {
        var voucher = await dbContext.Vouchers
            .Include(i => i.History)
            .AsNoTracking()
            .Where(v => v.Seat.Row == code)
            .FirstOrDefaultAsync(ct);
        var orderId = await dbContext.Orders
            .Where(o => o.BasketId == basketId)
            .Select(o => (long?)o.Id)
            .FirstOrDefaultAsync(ct);
        if (voucher is null)
        {
            return ValidationResultInternal.Invalid("Voucher not found");
        }

        if (orderId is null)
        {
            return ValidationResultInternal.Invalid("Order not found for the provided basket ID");
        }

        if (voucher.RemainingAmount <= 0)
        {
            return ValidationResultInternal.Invalid($"Voucher has insufficient funds (£{voucher.RemainingAmount:N2})");
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

        return ValidationResultInternal.Valid(voucher.Id, orderId.Value, discount);
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

    private class ValidationResultInternal
    {
        private ValidationResultInternal(ValidationResult result)
        {
            Result = result;
        }

        public long? VoucherId { get; init; }
        public long? OrderId { get; init; }

        public ValidationResult Result { get; }

        [MemberNotNullWhen(true, nameof(VoucherId))]
        [MemberNotNullWhen(true, nameof(OrderId))]
        public bool IsValid => Result.IsValid;

        public static ValidationResultInternal Valid(long voucherId, long orderId, decimal discount)
            => new(ValidationResult.Valid(discount)) { VoucherId = voucherId, OrderId = orderId };

        public static ValidationResultInternal Invalid(string errorMessage)
            => new(ValidationResult.Invalid(errorMessage)) { VoucherId = null };
    }

    public class ValidationResult
    {
        private ValidationResult()
        {
        }

        [MemberNotNullWhen(true, nameof(Discount))]
        public bool IsValid => Discount.HasValue;

        public decimal? Discount { get; init; }
        public string? ErrorMessage { get; init; }

        public static ValidationResult Valid(decimal? discount = null)
            => new() { Discount = discount };

        public static ValidationResult Invalid(string errorMessage)
            => new() { ErrorMessage = errorMessage };
    }
}