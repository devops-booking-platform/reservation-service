using Microsoft.AspNetCore.Authentication.JwtBearer;
using ReservationService.Infrastructure.Clients;
using ReservationService.Repositories.Implementations;
using ReservationService.Repositories.Interfaces;
using ReservationService.Services.Implementations;
using ReservationService.Services.Interfaces;
using ReservationServiceImpl = ReservationService.Services.Implementations.ReservationService;

namespace ReservationService.Infrastructure.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddReservationServiceDependencies(this IServiceCollection services, IConfiguration config)
		{
			services.AddHttpContextAccessor();
			services.AddScoped<ICurrentUserService, CurrentUserService>();
			services.AddHttpClient<IAccommodationClient, AccommodationClient>(http =>
			{
				http.BaseAddress = new Uri(config["Services:Accommodation:BaseUrl"]!);
			});
			services.AddScoped<IReservationService, ReservationServiceImpl>();
			services.AddScoped<IReservationRepository, ReservationRepository>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
			return services;
		}
		public static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
			{
				options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
				{
					Name = "Authorization",
					Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
					In = Microsoft.OpenApi.Models.ParameterLocation.Header,
					Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
					Scheme = JwtBearerDefaults.AuthenticationScheme,
					BearerFormat = "JWT"
				});

				options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
				{
					{
						new Microsoft.OpenApi.Models.OpenApiSecurityScheme
						{
							Reference = new Microsoft.OpenApi.Models.OpenApiReference
							{
								Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
								Id = JwtBearerDefaults.AuthenticationScheme
							}
						},
						Array.Empty<string>()
					}
				});
			});
			return services;
		}
	}

}
