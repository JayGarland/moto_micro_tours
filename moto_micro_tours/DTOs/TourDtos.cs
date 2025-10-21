namespace moto_micro_tours.DTOs;

/// <summary>
/// DTO for listing tours with their motards
/// </summary>
public class TourListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Aggregated info from TourMotards
    public int TotalCapacity { get; set; }
    public int TotalSeatsBooked { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int MotardCount { get; set; }

    public List<TourMotardSummaryDto> Motards { get; set; } = new();
}

/// <summary>
/// Summary of a motard in a tour (for list view)
/// </summary>
public class TourMotardSummaryDto
{
    public Guid TourMotardId { get; set; }
    public Guid MotardUserId { get; set; }
    public string MotardName { get; set; } = string.Empty;
    public int CapacitySeats { get; set; }
    public int SeatsBooked { get; set; }
    public decimal PricePerSeat { get; set; }
}

/// <summary>
/// Complete motard profile with reviews and statistics
/// </summary>
public class MotardProfileDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }

    // Profile details
    public string? Bio { get; set; }
    public string? BikeModel { get; set; }
    public string? LicenseNo { get; set; }

    // Statistics
    public decimal? AvgRating { get; set; }
    public int CompletedTours { get; set; }
    public int TotalReviews { get; set; }

    // Reviews from past participants
    public List<ReviewDto> Reviews { get; set; } = new();

    // Optional: Tours this motard is part of (upcoming)
    public List<MotardTourSummaryDto> UpcomingTours { get; set; } = new();
}

/// <summary>
/// Review left by a participant for a motard
/// </summary>
public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid BookingId { get; set; }
}

/// <summary>
/// Summary of a tour that a motard is part of
/// </summary>
public class MotardTourSummaryDto
{
    public Guid TourId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;
    public int AvailableSeats { get; set; }
    public decimal PricePerSeat { get; set; }
}

/// <summary>
/// Detailed tour info with full motard details
/// </summary>
public class TourDetailDto
{//todo: add datetime createdat
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public List<TourMotardDetailDto> Motards { get; set; } = new();
}

/// <summary>
/// Detailed motard info in a tour (includes profile preview)
/// </summary>
public class TourMotardDetailDto
{
    public Guid TourMotardId { get; set; }
    public Guid MotardUserId { get; set; }
    public string MotardName { get; set; } = string.Empty;
    public string? MotardBio { get; set; }
    public string? BikeModel { get; set; }
    public float? AvgRating { get; set; }
    public int CompletedTours { get; set; }

    public int CapacitySeats { get; set; }
    public int SeatsBooked { get; set; }
    public decimal PricePerSeat { get; set; }
}
