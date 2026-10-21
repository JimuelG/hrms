using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeIdentifiersConfiguration : IEntityTypeConfiguration<EmployeeIdentifiers>
{
    public void Configure(EntityTypeBuilder<EmployeeIdentifiers> builder)
    {
        builder.Property(i => i.TinProtected).HasMaxLength(500);
        builder.Property(i => i.SssProtected).HasMaxLength(500);
        builder.Property(i => i.PhilHealthProtected).HasMaxLength(500);
        builder.Property(i => i.PagIbigProtected).HasMaxLength(500);
        builder.Property(i => i.BankAccountNumberProtected).HasMaxLength(500);
        builder.Property(i => i.BankName).HasMaxLength(100);
        builder.Property(i => i.BankAccountName).HasMaxLength(150);

        builder.HasOne<Employee>().WithMany().HasForeignKey(i => i.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.EmployeeId).IsUnique();
    }
}

public class IdentifierAccessLogConfiguration : IEntityTypeConfiguration<IdentifierAccessLog>
{
    public void Configure(EntityTypeBuilder<IdentifierAccessLog> builder)
    {
        builder.Property(l => l.Field).HasConversion<int>();
        builder.Property(l => l.Action).HasConversion<int>();

        builder.HasOne<Employee>().WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => new { l.EmployeeId, l.CreatedAtUtc });
    }
}