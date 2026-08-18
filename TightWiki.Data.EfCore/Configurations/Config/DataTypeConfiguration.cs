using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="DataType"/> (Config.DataType).
    /// </summary>
    public class DataTypeConfiguration : IEntityTypeConfiguration<DataType>
    {
        public void Configure(EntityTypeBuilder<DataType> builder)
        {
            builder.ToTable("DataType", schema: "Config");

            builder.HasKey(e => e.Id);

            //Nullable in the real schema - no NOT NULL constraint is declared for this column, only the
            //unique index below.
            builder.Property(e => e.Name)
                .UseCollation("NOCASE");

            builder.HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}
