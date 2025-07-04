using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Order = POC.Api.Persistence.Entities.Order;

namespace POC.Api.Features.Vouchers.BuyVoucher;

public static class BuyVoucher
{
    public record Request(Guid? BasketId, decimal Price);

    public record Response(Guid BasketId);

    public class Endpoint(AppDbContext dbContext, VouchersService vouchersService) : Endpoint<Request, Response>
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
            var (orderId, basketId) = await EnsureOrderCreated(req.BasketId, ct);
            if (orderId is null)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            await vouchersService.BuyVoucherAsync(orderId.Value, req.Price, ct);

            var response = new Response(basketId);
            await SendAsync(response, StatusCodes.Status201Created, ct);
        }

        private async Task<(long? OrderId, Guid BasketId)> EnsureOrderCreated(Guid? requestBasketId, CancellationToken cancellationToken)
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
    }
}