using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class RecurringPayrollItemConfiguration : IEntityTypeConfiguration<RecurringPayrollItem>
{
    public void Configure(EntityTypeBuilder<RecurringPayrollItem> builder)
    {
        builder.Property(i => i.Label).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Amount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TotalOwed).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TotalOwed).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Tax).HasConversion<int>();
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasOne<Employee>().WithMany().HasForeignKey(i => i.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => new { i.EmployeeId, i.IsActive });
    }
}

public class LoanRepaymentConfiguration : IEntityTypeConfiguration<LoanRepayment>
{
    public void Configure(EntityTypeBuilder<LoanRepayment> builder)
    {
        builder.Property(r => r.Amount).HasColumnType("decimal(18,2)");

        builder.HasOne<RecurringPayrollItem>().WithMany().HasForeignKey(r => r.RecurringPayrollItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(r => r.PayrollRunId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.PayrollRunId, r.RecurringPayrollItemId }).IsUnique();
    }
}

public class PayrollLoanApplicationConfiguration : IEntityTypeConfiguration<PayrollLoanApplication>
{
    public void Configure(EntityTypeBuilder<PayrollLoanApplication> builder)
    {
        builder.Property(a => a.Amount).HasColumnType("decimal(18,2)");

        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(a => a.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RecurringPayrollItem>().WithMany().HasForeignKey(a => a.RecurringPayrollItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PayrollRunId, a.RecurringPayrollItemId }).IsUnique();
    }
}