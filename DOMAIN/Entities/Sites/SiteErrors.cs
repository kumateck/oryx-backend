using SHARED;

namespace DOMAIN.Entities.Sites;

public static class SiteErrors
{
    public static Error InvalidName(string name) =>
        Error.Validation("Sites.InvalidName", $"The site name: {name} is invalid");
}