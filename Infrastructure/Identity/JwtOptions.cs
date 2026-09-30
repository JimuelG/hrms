using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Identity;
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; set; } = "";
    [Required]
    public string Audience { get; set; } = "";
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "";
    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}