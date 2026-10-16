using System.ComponentModel.DataAnnotations;

namespace moto_micro_tours.Domain.Entities;

public class TourMotard
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string MotardUserId { get; set; } = null!;
    public int CapacitySeats { get; set; }
    public decimal PricePerSeat { get; set; }
    public int SeatsBooked { get; set; }

    public Tour? Tour { get; set; }
    public AppUser? Motard { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>
    /// Optimistic concurrency token, stamped centrally in AppDbContext.SaveChanges
    /// for every Added/Modified row. Configured with IsConcurrencyToken, so two
    /// concurrent payments competing for the last seat cannot both succeed.
    /// </summary>
    public DateTime RowVersion { get; set; }
}
