using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="RolePermission"/> (Users.RolePermission).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^006^Users^RolePermission.sql (this table has a
    /// real CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermission", schema: "Users");

            builder.HasKey(e => e.Id);

            //Same "named in SQL but not honored by SQLite" situation as AccountRole above - the live database
            //resolves this composite constraint to an unnamed autoindex (sqlite_autoindex_RolePermission_2), not
            //the "IX_Unique" name given in the CREATE TABLE script.
            builder.HasIndex(e => new { e.RoleId, e.PermissionId, e.Namespace, e.PageId, e.PermissionDispositionId })
                .IsUnique();

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.

            builder.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.PermissionDisposition)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionDispositionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
