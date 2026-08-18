using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="Permission"/> (Users.Permission).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^003^Users^Permission.sql (this table has a real
    /// CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permission", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            //Description carries no COLLATE NOCASE in the real schema, unlike Name.

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.
            builder.HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}
