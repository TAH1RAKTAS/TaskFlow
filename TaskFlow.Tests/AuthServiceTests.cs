using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Services;

namespace TaskFlow.Tests;

public class AuthServiceTests
{
    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TaskFlowTestKey_With_AtLeast_32_Characters!",
                ["Jwt:Issuer"] = "TaskFlow.Tests",
                ["Jwt:Audience"] = "TaskFlow.Tests.Users"
            })
            .Build();

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task RegisterAsync_NormalizesEmail()
    {
        await using var context = CreateContext();
        var service = new AuthService(context, CreateConfiguration());

        var result = await service.RegisterAsync(new RegisterDto
        {
            Email = "  Demo@Example.COM ",
            Password = "StrongPassword123!"
        });

        Assert.True(result);
        Assert.Equal("demo@example.com", (await context.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task RegisterAsync_SameEmailWithDifferentCase_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new AuthService(context, CreateConfiguration());
        var first = new RegisterDto { Email = "demo@example.com", Password = "StrongPassword123!" };
        var duplicate = new RegisterDto { Email = "DEMO@example.com", Password = "AnotherPassword123!" };

        await service.RegisterAsync(first);
        var result = await service.RegisterAsync(duplicate);

        Assert.False(result);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        await using var context = CreateContext();
        var service = new AuthService(context, CreateConfiguration());
        await service.RegisterAsync(new RegisterDto
        {
            Email = "demo@example.com",
            Password = "StrongPassword123!"
        });

        var token = await service.LoginAsync(new LoginDto
        {
            Email = "DEMO@example.com",
            Password = "StrongPassword123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}
