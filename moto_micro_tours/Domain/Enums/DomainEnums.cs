namespace moto_micro_tours.Domain.Enums;

public enum UserRoleType
{
    Participant,
    Motard
}

public enum TourStatus
{
    Draft,
    Published,
    Closed,
    Cancelled
}

public enum BookingStatus
{
    Pending,
    Approved,
    Declined,
    Cancelled,
    CancelledExpired,
    Paid,
    Refunded
}

public enum CancelDecision
{
    None,
    ApprovedByMotard,
    DeclinedByMotard
}

public enum PaymentStatus
{
    Initiated,
    Succeeded,
    Failed,
    Refunded
}

public enum PayoutStatus
{
    Pending,
    Settled,
    Failed
}
