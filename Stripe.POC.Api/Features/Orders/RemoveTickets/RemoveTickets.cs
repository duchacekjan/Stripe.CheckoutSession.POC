using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Common;
using POC.Api.DTOs;
using POC.Api.Features.Orders.Shared;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace POC.Api.Features.Orders.RemoveTickets;

public static class RemoveTickets
{
    public record Request(Guid BasketId, List<long> SeatIds);

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
            var response = await RemoveTicketsFromSession(req.BasketId, ticketsToRemove, ct);
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

        private async Task<Response> RemoveTicketsFromSession(Guid basketId, List<TicketDTO> tickets, CancellationToken cancellationToken)
        {
            var sessionId = await dbContext.CheckoutSessions
                .Where(w => w.Order.BasketId == basketId)
                .Select(s => s.SessionId)
                .FirstAsync(cancellationToken);
            try
            {
                var service = new SessionService();
                var lineItems = await service.LineItems.ListAsync(sessionId, cancellationToken: cancellationToken);
                var updatedItems = lineItems.Select(s => new SessionLineItemOptions
                {
                    Id = s.Id,
                    Quantity = s.Quantity
                }).ToList();
                foreach (var lineItem in lineItems)
                {
                    var priceBandTickets = lineItem.MatchingTickets(tickets);

                    if (priceBandTickets.Count == 0)
                    {
                        // No tickets to remove for this price band, keep the item
                        continue;
                    }

                    var existingItem = updatedItems.First(f => f.Id == lineItem.Id);
                    var newQuantity = lineItem.Quantity - priceBandTickets.Count;
                    if (newQuantity > 0)
                    {
                        existingItem.Quantity = newQuantity;
                    }
                    else
                    {
                        updatedItems.Remove(existingItem);
                    }
                    
                    tickets.RemoveAll(r => priceBandTickets.Any(a => a.SeatId == r.SeatId));
                }

                if (updatedItems.Count == 0)
                {
                    return new Response(UpdateStatus.Emptied);
                }

                var updateOptions = new SessionUpdateOptions
                {
                    LineItems = updatedItems
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