using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Logging;

namespace TightWiki.Data.EfCore.Configurations.Logging
{
    /// <summary>
    /// Fluent configuration for <see cref="Severity"/> (Logging.Severity).
    /// </summary>
    public class SeverityConfiguration : IEntityTypeConfiguration<Severity>
    {
        public void Configure(EntityTypeBuilder<Severity> builder)
        {
            builder.ToTable("Severity", schema: "Logging");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}
