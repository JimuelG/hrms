using Core.DTOs.Organization;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Settings;

public class TenantSettingsService(
    IUnitOfWork unit,
    IGenericRepository<TenantSettings> repo) : ITenantSettingsService
{
    public async Task<TenantSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await repo.GetEntityWithSpec(new TenantSettingsSpecification(), ct);

        if (settings is null)
        {
            settings = new TenantSettings();
            repo.Add(settings);
            await unit.Complete();
        }

        return ToDto(settings);
    }

    public async Task<TenantSettingsDto> UpdateAsync(UpdateTenantSettingsDto dto, CancellationToken ct = default)
    {
        var settings = await repo.GetEntityWithSpec(new TenantSettingsSpecification(),ct);

        if (settings is null)
        {
            settings = new TenantSettings();
            repo.Add(settings);
        }

        settings.CompanyLegalName = dto.CompanyLegalName;
        settings.LogoUrl = dto.LogoUrl;
        settings.Primarycolor = dto.PrimaryColor;
        settings.AddressLine = dto.AddressLine;
        settings.City = dto.City;
        settings.Country = dto.Country;
        settings.ContactEmail = dto.ContactEmail;
        settings.ContactPhone = dto.ContactPhone;
        settings.TimeZoneId = dto.TimeZoneId;
        settings.Currency = dto.Currency;
        settings.DateFormat = dto.DateFormat;
        settings.WorkingDays = (WorkingDay)dto.WorkingDays;

        settings.UpdatedAtUtc = DateTime.UtcNow;
        await unit.Complete();
        return ToDto(settings);
    }

    private static TenantSettingsDto ToDto(TenantSettings s) => new(
        s.CompanyLegalName,
        s.LogoUrl,
        s.Primarycolor,
        s.AddressLine,
        s.City,
        s.Country,
        s.ContactEmail,
        s.ContactPhone,
        s.TimeZoneId,
        s.Currency,
        s.DateFormat,
        (int)s.WorkingDays,
        s.UpdatedAtUtc
    );
}