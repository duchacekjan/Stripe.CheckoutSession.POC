using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.RemoveTickets;

public static class RemoveTickets
{
    public record Request(Guid BasketId, string SessionId, List<long> SeatIds);

    public record Response(UpdateStatus Status, string? Message = null);

    public enum UpdateStatus
    {
        Updated,
        Emptied,
        Error
    }

    public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/{basketId}/remove-tickets");
            Group<OrdersGroup>();
            Description(d => d
                .Produces<Response>()
                .Produces<string>()
                .WithName($"{nameof(Orders)}.{nameof(RemoveTickets)}")
            );
            Summary(s =>
            {
                s.Summary = "Removes tickets from and order";
                s.Responses[StatusCodes.Status200OK] = "Tickets removed successfully";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            if (await dbContext.Orders.Where(w => w.BasketId == req.BasketId).AnyAsync(ct) == false)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            var tickets = await dbContext.OrderTicketsAsync(req.BasketId, ct);
            var ticketsToRemove = tickets
                .Where(w => req.SeatIds.Contains(w.SeatId))
                .ToList();

            await RemoveTicketsFromOrder(ticketsToRemove, ct);
            var response = await RemoveTicketsFromSession(req.SessionId, ticketsToRemove, ct);
            switch (response.Status)
            {
                case UpdateStatus.Emptied:
                    await dbContext.Orders
                        .Where(w => w.BasketId == req.BasketId)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.Status, OrderStatus.Cancelled), cancellationToken: ct);
                    break;
                case UpdateStatus.Error:
                    await SendAsync(response, StatusCodes.Status400BadRequest, ct);
                    return;
                case UpdateStatus.Updated:
                    await SendOkAsync(response, ct);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task RemoveTicketsFromOrder(List<TicketDTO> tickets, CancellationToken cancellationToken)
        {
            var seatIds = tickets.Select(s => s.SeatId).ToList();
            var seatsToUpdate = await dbContext.Seats
                .Where(w => seatIds.Contains(w.Id))
                .ToListAsync(cancellationToken);
            foreach (var seat in seatsToUpdate)
            {
                seat.OrderItemId = null;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<Response> RemoveTicketsFromSession(string sessionId, List<TicketDTO> tickets, CancellationToken cancellationToken)
        {
            try
            {
                var service = new SessionService();
                var lineItems = await service.LineItems.ListAsync(sessionId, cancellationToken: cancellationToken);
                var remainingItems = new Dictionary<string, long?>();
                foreach (var lineItem in lineItems)
                {
                    // if (!lineItem.Metadata.TryGetValue("eventId", out var eventId) ||
                    //     !lineItem.Metadata.TryGetValue("performanceId", out var performanceId) ||
                    //     !lineItem.Metadata.TryGetValue("priceId", out var priceId))
                    // {
                    //     // Skip this item, will remain
                    //     remainingItems[lineItem.Id] = lineItem.Quantity;
                    //     continue;
                    // }
                    //
                    // var priceBandTickets = tickets.Where(w => w.EventId.ToString() == eventId &&
                    //                                           w.PerformanceId.ToString() == performanceId &&
                    //                                           w.PriceId.ToString() == priceId).ToList();

                    //PSEUDO MAP: Not ensured mapped only by price band
                    var lineItemPrice = lineItem.Price.UnitAmount / 100m;
                    var priceBandTickets = tickets
                        .Where(w => w.Price == lineItemPrice)
                        .ToList();
                    //END PSEUDO MAP
                    if (priceBandTickets.Count == 0)
                    {
                        // No tickets to remove for this price band, keep the item
                        remainingItems[lineItem.Id] = lineItem.Quantity;
                        continue;
                    }

                    var newQuantity = lineItem.Quantity - priceBandTickets.Count;

                    remainingItems[lineItem.Id] = newQuantity;
                }

                if (remainingItems.Count(w => w.Value > 0) == 0)
                {
                    return new Response(UpdateStatus.Emptied);
                }

                var updateOptions = new SessionUpdateOptions
                {
                    LineItems = remainingItems.Where(w => w.Value > 0).Select(item => new SessionLineItemOptions
                    {
                        Id = item.Key,
                        Quantity = item.Value
                    }).ToList()
                };
                await service.UpdateAsync(sessionId, updateOptions, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                return new Response(UpdateStatus.Error, e.Message);
            }

            return new Response(UpdateStatus.Updated);
        }
    }
}