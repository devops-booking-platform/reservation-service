using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ReservationService.Common.Events;
using ReservationService.Data;
using ReservationService.Infrastructure.Clients;

namespace ReservationService.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IEventBus> EventBusMock { get; } = new();
    public Mock<IAccommodationClient> AccommodationClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
            
            // Replace event bus with mock
            services.RemoveAll(typeof(IEventBus));
            services.AddSingleton(EventBusMock.Object);

            // Replace accommodation client with mock
            services.RemoveAll(typeof(IAccommodationClient));
            services.AddSingleton(AccommodationClientMock.Object);

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to get the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }
}