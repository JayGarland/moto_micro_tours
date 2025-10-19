using Microsoft.AspNetCore.Identity;

namespace moto_micro_tours.Domain.Entities;

public class AppUser : IdentityUser
{
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    public MotardProfile? MotardProfile { get; set; }
}
