namespace moto_micro_tours.Domain.Entities;

public class MotardProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string? Bio { get; set; }
    public string? BikeModel { get; set; }
    public string LicenseNo { get; set; } = null!;
    public decimal? AvgRating { get; set; }
    public int CompletedTours { get; set; }

    public AppUser? User { get; set; }
    public ICollection<TourMotard> TourMemberships { get; set; } = new List<TourMotard>();
}
