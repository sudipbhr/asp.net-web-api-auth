using Microsoft.AspNetCore.Identity;
using System.Security.Principal;
namespace Ecommerce.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string? MobileNumber { get; set; }
        public string? Address { get; set; }

    }
}
