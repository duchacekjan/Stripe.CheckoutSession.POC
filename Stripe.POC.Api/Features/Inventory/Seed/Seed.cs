using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using POC.Api.Persistence;
using POC.Api.Persistence.Entities;

namespace POC.Api.Features.Inventory.Seed;

public static class Seed
{
    public const long BookingProtectionPriceId = -1;

    public static readonly Event Voucher = new()
    {
        Id = -1,
        Name = "Voucher",
        Performances = new List<Performance>
        {
            new()
            {
                Id = -1,
                PerformanceDate = DateTime.MaxValue,
                DurationMinutes = 1
            }
        }
    };

    public static readonly Event BookingProtection = new()
    {
        Id = -2,
        Name = "Booking Protection",
        Performances = new List<Performance>
        {
            new()
            {
                Id = -2,
                PerformanceDate = DateTime.MaxValue,
                DurationMinutes = 1
            }
        }
    };
    public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<EmptyResponse>
    {
        public override void Configure()
        {
            Post("/seed");
            Group<InventoryGroup>();
            Description(d => d
                .Produces<EmptyResponse>()
                .Produces<string>()
                .WithName($"{nameof(Inventory)}.{nameof(Seed)}")
            );
            Summary(s =>
            {
                s.Summary = "Seeds inventory data for testing purposes. Removes all current data and adds new.";
                s.Responses[StatusCodes.Status200OK] = "Database seeded";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            await dbContext.Database.EnsureDeletedAsync(ct);
            await dbContext.Database.EnsureCreatedAsync(ct);

            dbContext.Prices.AddRange(PriceSeed);
            await dbContext.SaveChangesAsync(ct);
            Seed(dbContext.Events);
            await dbContext.SaveChangesAsync(ct);

            await SendOkAsync(new EmptyResponse(), ct);
        }

        private static void Seed(DbSet<Event> events)
        {
            if (!events.Any())
            {
                events.AddRange(EventsSeed());
            }
        }

        private static IEnumerable<Performance> GeneratePerformances(long eventId, int numberOfPerformances, uint durationMinutes = 120)
        {
            var startId = eventId * 10_000;
            var rnd = new Random();
            for (var i = 0; i < numberOfPerformances; i++)
            {
                var id = startId + i;
                yield return new Performance
                {
                    Id = id,
                    PerformanceDate = DateTime.Today.AddDays(i).AddHours(rnd.Next(12, 19)).AddMinutes(rnd.Next(0, 1) == 1 ? 0 : 30),
                    DurationMinutes = durationMinutes,
                    EventId = eventId,
                    Seats = GenerateSeats(id, rnd.Next(10, 15))
                };
            }
        }

        private static List<Seat> GenerateSeats(long performanceId, int seatsPerRow)
        {
            var row = Row.Start;
            var result = new List<Seat>();
            foreach (var price in PriceSeed.Where(w => w.Id > 0))
            {
                result.AddRange(GenerateRowOfSeatsForPrice(performanceId, row, price, seatsPerRow));
                row++;
            }

            return result;
        }

        private static IEnumerable<Seat> GenerateRowOfSeatsForPrice(long performanceId, Row row, Price price, int seatsPerPrice)
        {
            for (var i = 0; i < seatsPerPrice; i++)
            {
                yield return new Seat
                {
                    Row = row,
                    Number = (uint)(i + 1),
                    PriceId = price.Id,
                    PerformanceId = performanceId
                };
            }
        }

        private static Dictionary<long, string> EventDictionary => new()
        {
            { 1, "The Phantom of the Opera" },
            { 2, "Les Misérables" },
            { 3, "Hamilton" },
            { 4, "The Lion King" },
            { 5, "Wicked" },
            { 6, "Mamma Mia!" },
            { 7, "Chicago" }
        };

        private static IEnumerable<Event> EventsSeed()
        {
            var rnd = new Random();
            yield return Voucher;
            yield return BookingProtection;
            foreach (var pair in EventDictionary)
            {
                yield return new Event
                {
                    Id = pair.Key,
                    Name = pair.Value,
                    Performances = GeneratePerformances(pair.Key, rnd.Next(5, 20), (uint)rnd.Next(60, 180)).ToList()
                };
            }
        }

        private static List<Price> PriceSeed =>
        [
            new() { Id = BookingProtectionPriceId, Name = "Booking Protection", Amount = 5.00m },
            new() { Id = 1, Name = "Standard", Amount = 50.00m },
            new() { Id = 2, Name = "VIP", Amount = 100.00m },
            new() { Id = 3, Name = "Balcony", Amount = 75.00m },
            new() { Id = 4, Name = "Box", Amount = 150.00m },
            new() { Id = 5, Name = "Student Discount", Amount = 30.00m },
            new() { Id = 6, Name = "Obstructed View", Amount = 30.00m },
        ];

        public class Row
        {
            private const int StartingRow = 'A';
            private int Value { get; init; }
            private string Name => ((char)Value).ToString();

            private Row()
            {
            }

            public static Row Start => new() { Value = StartingRow };
            public static implicit operator string(Row row) => row.Name;
            public static implicit operator int(Row row) => row.Value;
            public static implicit operator Row(int rowValue) => new() { Value = rowValue };
        }
    }
}