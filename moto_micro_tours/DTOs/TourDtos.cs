namespace moto_micro_tours.DTOs;

// NOTE: Tour entity is missing these fields in the database:
// TODO (Database Schema): Add Description field (string?) for tour overview/details
// TODO (Database Schema): Add EndAt field (DateTime) to calculate tour duration
// TODO (Database Schema): Add CreatedAt field (DateTime) for tracking tour creation
// These should be added to the Tour entity, then reflected in all DTOs below.

/// <summary>
/// Base class for tour information shared across DTOs
/// </summary>
public abstract class TourBaseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for listing tours with their motards
/// Used in tour list/search views
/// </summary>
public class TourListDto : TourBaseDto
{
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
    public int CompletedToursCount { get; set; } //Optimize: use backend calculation
    public int TotalReviews { get; set; }

    // Reviews from past participants
    public List<ReviewDto> Reviews { get; set; } = new();

    // Optional: Tours this motard is part of (upcoming)
    public List<MotardTourSummaryDto> UpcomingTours { get; set; } = new();

    // Optional: Tours this motard has completed
    public List<MotardTourSummaryDto> CompletedTours { get; set; } = new();

}

/// <summary>
/// Summary of a tour that a motard is part of
/// Used in MotardProfileDto to show upcoming/completed tours
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
/// Used for tour detail page
/// </summary>
public class TourDetailDto : TourBaseDto
{
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

    public int CapacitySeats { get; set; }
    public int SeatsBooked { get; set; }
    public decimal PricePerSeat { get; set; }
}

/// <summary>
/// DTO for creating a new tour (Stage F: Only Motards can create)
/// </summary>
public class CreateTourDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Creator's motard details for the tour
    public int CapacitySeats { get; set; }
    public decimal PricePerSeat { get; set; }

    // Optional: Start as Published or Draft (default: Draft)
    public bool PublishImmediately { get; set; } = false;
}

/// <summary>
/// Response after creating a tour
/// </summary>
public class CreateTourResponseDto
{
    public Guid TourId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CreatorTourMotardId { get; set; }
    public string Message { get; set; } = string.Empty;
}
