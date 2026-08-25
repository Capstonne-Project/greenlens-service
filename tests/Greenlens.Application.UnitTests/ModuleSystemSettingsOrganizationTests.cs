using FluentAssertions;
using Greenlens.Application.BusinessRules;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class ModuleSystemSettingsOrganizationTests
{
    [Fact]
    public void ContractWarningDays_ValidJson_ReturnsSortedDistinctPositiveDays_BR_CMP_007()
    {
        var settings = new ConfigurableSystemSettingsProvider(
            (SystemSettingModule.Organization, SystemSettingKeys.Organization.ContractWarningDays, "[7,30,7,0,-1]"));

        var days = ModuleSystemSettings.ContractWarningDays(settings);

        days.Should().Equal(30, 7);
    }

    [Fact]
    public void ContractWarningDays_InvalidJson_FallsBackToDefault_BR_CMP_007()
    {
        var settings = new ConfigurableSystemSettingsProvider(
            (SystemSettingModule.Organization, SystemSettingKeys.Organization.ContractWarningDays, "not-json"));

        ModuleSystemSettings.ContractWarningDays(settings).Should().Equal(30, 7, 1);
    }

    [Fact]
    public void ContractAlertHorizonDays_UsesLargestWarningWindow_BR_CMP_007()
    {
        var settings = new ConfigurableSystemSettingsProvider(
            (SystemSettingModule.Organization, SystemSettingKeys.Organization.ContractWarningDays, "[14,3]"));

        ModuleSystemSettings.ContractAlertHorizonDays(settings).Should().Be(14);
    }

    private sealed class ConfigurableSystemSettingsProvider(
        params (SystemSettingModule Module, string Key, string Value)[] entries)
        : ISystemSettingsProvider
    {
        public int GetInt(SystemSettingModule module, string key, int fallback) => fallback;

        public decimal GetDecimal(SystemSettingModule module, string key, decimal fallback) => fallback;

        public bool GetBool(SystemSettingModule module, string key, bool fallback) => fallback;

        public string GetString(SystemSettingModule module, string key, string fallback)
        {
            foreach (var (entryModule, entryKey, value) in entries)
            {
                if (entryModule == module && entryKey == key)
                    return value;
            }

            return fallback;
        }
    }
}
