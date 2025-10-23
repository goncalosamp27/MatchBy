namespace MatchBy.Models;

public class Team
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }
    public ICollection<ApplicationUser> Members { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
