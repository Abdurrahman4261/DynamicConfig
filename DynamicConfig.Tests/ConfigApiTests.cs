using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicConfig.Library.Context;
using DynamicConfig.Library.Entities;
using FluentAssertions;
using MongoDB.Driver;
using Moq;

public class ConfigApiTests
{
    private Mock<IMongoCollection<ConfigurationItem>> CreateMockCollection()
    {
        var mockCollection = new Mock<IMongoCollection<ConfigurationItem>>();
        var mockCursor = new Mock<IAsyncCursor<ConfigurationItem>>();

        var configItems = new[]
        {
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
            }
        };

        mockCursor.SetupSequence(cursor => cursor.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  
            .ReturnsAsync(false); 

        mockCursor.SetupGet(cursor => cursor.Current).Returns(configItems.AsQueryable());

        mockCollection.Setup(collection => collection.FindAsync(
            It.IsAny<FilterDefinition<ConfigurationItem>>(),
            It.IsAny<FindOptions<ConfigurationItem>>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(mockCursor.Object);

        return mockCollection;
    }

    private Mock<MongoDbContext> CreateMockDbContext()
    {
        var mockCollection = CreateMockCollection();

        var mockDatabase = new Mock<IMongoDatabase>();
        mockDatabase.Setup(db => db.GetCollection<ConfigurationItem>("ConfigurationItems", null))
            .Returns(mockCollection.Object);

        var mockContext = new Mock<MongoDbContext>(mockDatabase.Object);
        return mockContext;
    }

    [Fact(DisplayName = "SERVICE-A için yalnýzca aktif kayýtlar dönmeli")]
    public async Task GetActiveConfigurations_ShouldReturnOnlyActive_ForSpecificApplication()
    {
        var mockContext = CreateMockDbContext();
        var context = mockContext.Object;

        var filter = Builders<ConfigurationItem>.Filter.Eq(c => c.ApplicationName, "SERVICE-A") &
                     Builders<ConfigurationItem>.Filter.Eq(c => c.IsActive, true);
        var activeConfigs = await context.ConfigurationItems
            .Find(filter)
            .ToListAsync();

        activeConfigs.Should().HaveCount(1, "çünkü sadece bir kayýt aktif");
        activeConfigs[0].Name.Should().Be("SiteName");
        activeConfigs[0].Value.Should().Be("soty.io");
        activeConfigs[0].Type.Should().Be("string");
    }

    [Fact(DisplayName = "SERVICE-B için kayýtlar doðru dönmeli")]
    public async Task GetConfigurations_For_ServiceB_ShouldReturnExpectedValues()
    {
        var mockContext = CreateMockDbContext();
        var context = mockContext.Object;

        var filter = Builders<ConfigurationItem>.Filter.Eq(c => c.ApplicationName, "SERVICE-B") &
                     Builders<ConfigurationItem>.Filter.Eq(c => c.IsActive, true);
        var configs = await context.ConfigurationItems
            .Find(filter)
            .ToListAsync();

        configs.Should().ContainSingle()
            .Which.Name.Should().Be("IsBasketEnabled");
    }

    [Fact(DisplayName = "Aktif olmayan kayýtlar filtrelenmeli")]
    public async Task InactiveConfigurations_ShouldBeExcluded()
    {
        var mockContext = CreateMockDbContext();
        var context = mockContext.Object;

        var filter = Builders<ConfigurationItem>.Filter.Eq(c => c.IsActive, true);
        var allActive = await context.ConfigurationItems
            .Find(filter)
            .ToListAsync();

        allActive.Should().NotContain(c => c.Name == "MaxItemCount");
    }
}
