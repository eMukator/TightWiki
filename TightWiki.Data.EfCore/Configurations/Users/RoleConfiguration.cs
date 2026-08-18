using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="Role"/> (Users.Role).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^002^Users^Role.sql. Unlike the other tables with
    /// a real CREATE TABLE script in the Users schema, this one only adds a column (IsBuiltIn) to a table that
    /// already existed before 2.26.0 - the base Id/Name/Description columns predate the scripted history (see
    /// Database-Providers-Plan.md chapter 2.1a) and were verified against the live database instead.
    /// </remarks>
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description)
                .UseCollation("NOCASE");

            builder.Property(e => e.IsBuiltIn)
                .HasDefaultValue(true);

            builder.HasIndex(e => e.Name, "IX_Role_Name")
                .IsUnique();
        }
    }
}
