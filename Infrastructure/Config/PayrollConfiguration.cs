using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class PayrollSettingsConfiguration : IEntityTypeConfiguration<PayrollSettingsRecord>
{
    public void Configure(EntityTypeBuilder<PayrollSettingsRecord> builder)
    {
        builder.ToTable("PayrollSettings");
        builder.Property(s => s.Frequency).HasConversion<int>();
        builder.Property(s => s.ContributionPolicy).HasConversion<int>();
        builder.Property(s => s.HoursPerDay).HasColumnType("decimal(4,2)");
        builder.Property(s => s.OrdinaryOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(s => s.RestDayOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(s => s.HolidayOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(s => s.ThirteenthMonthExemptCeiling).HasColumnType("decimal(18,2)");
        builder.Property(s => s.PartialMonthContribution).HasConversion<int>();
        builder.Property(s => s.TaxMethod).HasConversion<int>();

        builder.HasIndex(s => s.TenantId).IsUnique();
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Frequency).HasConversion<int>();
        builder.Property(r => r.ContributionPolicy).HasConversion<int>();
        builder.Property(r => r.HoursPerDay).HasColumnType("decimal(4,2)");
        builder.Property(r => r.OrdinaryOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(s => s.RestDayOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(s => s.HolidayOvertimeMultiplier).HasColumnType("decimal(5,3)");
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.CancellationReason).HasMaxLength(500);
        builder.Property(r => r.RowVersion).IsRowVersion();
        builder.Property(r => r.PartialMonthContribution).HasConversion<int>();
        builder.Property(r => r.TaxMethod).HasConversion<int>();
        builder.Property(r => r.Kind).HasConversion<int>();
        builder.Property(r => r.ThirteenthMonthExemptCeiling).HasColumnType("decimal(18,2)");

        builder.HasMany(r => r.Results).WithOne(x => x.Run).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Adjustments).WithOne(x => x.Run).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.PeriodStart, r.PeriodEnd}).IsUnique().HasFilter("[Status] <> 4");
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.TenantId, r.Kind, r.PeriodStart, r.PeriodEnd }).IsUnique().HasFilter("[Status] <> 4");
    }
}

public class PayrollResultRecordConfiguration : IEntityTypeConfiguration<PayrollResultRecord>
{
    public void Configure(EntityTypeBuilder<PayrollResultRecord> builder)
    {
        builder.Property(r => r.EmployeeNumber).HasMaxLength(30).IsRequired();
        builder.Property(r => r.EmployeeName).HasMaxLength(250).IsRequired();
        builder.Property(r => r.DepartmentName).HasMaxLength(100);
        builder.Property(r => r.PositionTitle).HasMaxLength(100);
        builder.Property(r => r.ErrorMessage).HasMaxLength(500);

        foreach (var p in new [] { nameof(PayrollResultRecord.MonthlyBasic), nameof(PayrollResultRecord.Gross),
                        nameof(PayrollResultRecord.TaxableIncome),
                        nameof(PayrollResultRecord.TotalEarnings),
                        nameof(PayrollResultRecord.TotalDeductions),
                        nameof(PayrollResultRecord.Net),
                        nameof(PayrollResultRecord.EmployerContributions) })
                builder.Property(p).HasColumnType("decimal(18,2)");
        
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.PayrollRunId, r.EmployeeId }).IsUnique();
        builder.HasIndex(r => r.EmployeeId);
    }
}

public class PayrollAdjustmentRecordConfiguration : IEntityTypeConfiguration<PayrollAdjustmentRecord>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustmentRecord> builder)
    {
        builder.Property(a => a.Label).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Amount).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Tax).HasConversion<int>();

        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.PayrollRunId, a.EmployeeId });
    }
}