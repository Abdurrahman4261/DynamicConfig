using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConfigApiTests
{
    private MongoDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MongoDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new MongoDbContext(options);

        context.ConfigurationItems.AddRange(
            new ConfigurationItem
            {
                Name = "SiteName",
                Type = "string",
                Value = "soty.io",
                IsActive = true,
                ApplicationName = "SERVICE-A"
            },
            new ConfigurationItem
            {
                Name = "IsBasketEnabled",
                Type = "bool",
                Value = "1",
                IsActive = true,
                ApplicationName = "SERVICE-B"
            },
            new ConfigurationItem
            {
                Name = "MaxItemCount",
                Type = "int",
                Value = "50",
                IsActive = false,
                ApplicationName = "SERVICE-A"
            });

        context.SaveChanges();
        return context;
    }

    [Fact(DisplayName = "SERVICE-A için yalnýzca aktif kayýtlar dönmeli")]
    public async Task GetActiveConfigurations_ShouldReturnOnlyActive_ForSpecificApplication()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act
        var activeConfigs = await context.ConfigurationItems
            .Where(c => c.ApplicationName == "SERVICE-A" && c.IsActive)
            .ToListAsync();

        // Assert
        activeConfigs.Should().HaveCount(1, "çünkü sadece bir kayýt aktif");
        activeConfigs[0].Name.Should().Be("SiteName");
        activeConfigs[0].Value.Should().Be("soty.io");
        activeConfigs[0].Type.Should().Be("string");
    }

    [Fact(DisplayName = "SERVICE-B için kayýtlar doðru dönmeli")]
    public async Task GetConfigurations_For_ServiceB_ShouldReturnExpectedValues()
    {
        using var context = CreateInMemoryDbContext();

        var configs = await context.ConfigurationItems
            .Where(c => c.ApplicationName == "SERVICE-B" && c.IsActive)
            .ToListAsync();

        configs.Should().ContainSingle()
            .Which.Name.Should().Be("IsBasketEnabled");
    }

    [Fact(DisplayName = "Aktif olmayan kayýtlar filtrelenmeli")]
    public async Task InactiveConfigurations_ShouldBeExcluded()
    {
        using var context = CreateInMemoryDbContext();

        var allActive = await context.ConfigurationItems
            .Where(c => c.IsActive)
            .ToListAsync();

        allActive.Should().NotContain(c => c.Name == "MaxItemCount");
    }
}
