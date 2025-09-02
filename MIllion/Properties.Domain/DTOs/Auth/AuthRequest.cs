using System.ComponentModel.DataAnnotations;

namespace Properties.Domain.DTOs.Auth
{
    public class AuthRequest
    {
        [Required]
        public string EmailOrUsername { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
    }
}
