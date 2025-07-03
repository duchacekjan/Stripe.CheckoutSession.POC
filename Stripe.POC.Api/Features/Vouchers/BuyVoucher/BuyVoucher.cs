using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using POC.Api.DTOs;
using POC.Api.Features.Inventory.Seed;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Order = POC.Api.Persistence.Entities.Order;

namespace POC.Api.Features.Vouchers.BuyVoucher;

public static class BuyVoucher
{
    public record Request(Guid? BasketId, decimal Price);

    public record Response(Guid BasketId);

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/buy");
            Group<VouchersGroup>();
            Description(d => d
                .Produces<EmptyResponse>()
                .Produces<string>(StatusCodes.Status201Created)
                .WithName($"{nameof(Vouchers)}.{nameof(BuyVoucher)}")
            );
            Summary(s =>
            {
                s.Summary = "Client secret for created checkout session";
                s.Responses[StatusCodes.Status201Created] = "Successfully created checkout session";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var (orderId, basketId) = await GetOrderId(req.BasketId, ct);
            if (orderId is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var priceId = await GetPriceId(req.Price, ct);
            await CreateVoucher(orderId.Value, priceId, ct);

            var response = new Response(basketId);
            await SendAsync(response, StatusCodes.Status201Created, ct);
        }

        private async Task CreateVoucher(long orderId, long priceId, CancellationToken ct)
        {
            var orderItem = await dbContext.OrderItems
                .Where(w => w.OrderId == orderId)
                .Where(w => w.Seats.Any(s => s.PerformanceId == Seed.Voucher.Performances.First().Id && s.PriceId == priceId))
                .FirstOrDefaultAsync(ct);

            if (orderItem is null)
            {
                orderItem = new OrderItem
                {
                    OrderId = orderId,
                    Seats = []
                };
                dbContext.OrderItems.Add(orderItem);
            }

            orderItem.Seats.Add(new Seat
            {
                Row = Guid.NewGuid().ToString(),
                Number = (uint)Math.Abs(orderItem.Seats.Count),
                PriceId = priceId,
                PerformanceId = Seed.Voucher.Performances.First().Id,
            });
            await dbContext.SaveChangesAsync(ct);
        }

        private async Task<(long? OrderId, Guid BasketId)> GetOrderId(Guid? requestBasketId, CancellationToken cancellationToken)
        {
            long? orderId;
            Guid basketId = requestBasketId ?? Guid.NewGuid();
            if (requestBasketId is null)
            {
                var order = new Order
                {
                    BasketId = basketId,
                    Status = OrderStatus.Created
                };
                var entity = dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync(cancellationToken);
                orderId = entity.Entity.Id;
            }
            else
            {
                orderId = await dbContext.Orders.Where(w => w.BasketId == requestBasketId)
                    .Select(s => (long?)s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return (orderId, basketId);
        }

        private async Task<long> GetPriceId(decimal price, CancellationToken cancellationToken)
        {
            var existingPrice = await dbContext.Prices
                .Where(p => p.Name.StartsWith(VouchersGroup.VoucherPrefix))
                .Where(p => p.Amount == price)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPrice is not null)
            {
                return existingPrice.Id;
            }

            var newPrice = new Price { Amount = price, Name = $"{VouchersGroup.VoucherPrefix}{price:N3}" };
            var entity = dbContext.Prices.Add(newPrice).Entity;
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }

        // private async Task AddVoucherToCheckoutSession(Guid basketId, CancellationToken ct)
        // {
        //     var session = await dbContext.CheckoutSessions
        //         .Where(w => w.Order.BasketId == basketId)
        //         .FirstOrDefaultAsync(ct);
        //
        //     if (session is null)
        //     {
        //         return;
        //     }
        //
        //     var voucher = new Seat
        //     {
        //         Row = Guid.NewGuid().ToString(),
        //         Number = 0,
        //         PerformanceId = Seed.Voucher.Performances.First().Id,
        //         OrderItemId = session.OrderItemId
        //     };
        //     dbContext.Seats.Add(voucher);
        //     await dbContext.SaveChangesAsync(ct);
        // }
    }
}