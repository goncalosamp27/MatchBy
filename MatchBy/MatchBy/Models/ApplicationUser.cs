using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using MatchBy.Enums;

namespace MatchBy.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; }
    public ICollection<Sports> PreferredSports { get; set; } = [];
    public Location? BaseLocation { get; set; }
    public string? Bio { get; set; }
    public float Rating { get; set; }
    public AccountStatus Status { get; set; }
    public FileStore? ProfileImage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

public record Location(double Latitude, double Longitude, string City, string Country);
public record FileStore(string? Url, string Key, DateTime? ExpireDateTime, StorageType StorageType, FileCategory FileCategory, FileType FileType);
