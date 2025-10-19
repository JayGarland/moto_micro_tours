using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

public class UserRole
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public UserRoleType Role { get; set; }

    public AppUser? User { get; set; }
}
