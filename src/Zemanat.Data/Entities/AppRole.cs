using Microsoft.AspNetCore.Identity;

namespace Zemanat.Data.Entities;

/// <summary>A role. The name is fixed; the title shown to people can be edited by the manager.</summary>
public class AppRole : IdentityRole<int>
{
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
