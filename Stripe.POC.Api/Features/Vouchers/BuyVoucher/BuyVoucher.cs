using FastEndpoints;
using Microsoft.EntityFrameworkCore;
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
            long? orderId = null;
            Guid basketId = req.BasketId ?? Guid.NewGuid();
            if (req.BasketId is null)
            {
                var order = new Order
                {
                    BasketId = basketId,
                    Status = OrderStatus.Created
                };
                var entity = dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync(ct);
                orderId = entity.Entity.Id;
            }
            else
            {
                orderId = await dbContext.Orders.Where(w => w.BasketId == req.BasketId)
                    .Select(s => (long?)s.Id)
                    .FirstOrDefaultAsync(ct);
                if (orderId is null)
                {
                    await SendNotFoundAsync(ct);
                    return;
                }
            }

            var priceId = await GetPriceId(req.Price, ct);
            var orderItem = new OrderItem
            {
                OrderId = orderId.Value,
                Seats =
                [
                    new Seat
                    {
                        Row = string.Empty, // Vouchers do not have a row they have code. Generated upon payment
                        Number = 0,
                        PriceId = priceId,
                        PerformanceId = -1 // Voucher-specific performance
                    }
                ]
            };
            dbContext.OrderItems.Add(orderItem);
            await dbContext.SaveChangesAsync(ct);

            var response = new Response(basketId);
            await SendAsync(response, StatusCodes.Status201Created, ct);
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
    }
}