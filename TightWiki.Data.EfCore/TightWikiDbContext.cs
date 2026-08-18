using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore
{
    /// <summary>
    /// Provider-agnostic shared EF Core model for TightWiki. Entities and Fluent configuration are added in
    /// follow-up work; this is intentionally a bare skeleton for now.
    /// </summary>
    public class TightWikiDbContext : DbContext
    {
    }
}
