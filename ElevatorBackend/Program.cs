using ElevatorBackend.Data;
using ElevatorBackend.Hubs;
using ElevatorBackend.Repositories;
using ElevatorBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization; 
using Microsoft.AspNetCore.Builder; 

namespace ElevatorBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Prevents JSON serialization reference loop
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                });

            builder.Services.AddSignalR();

            // CORS support
            builder.Services.AddCors(options =>
            {
                options.AddPolicy( "CORSPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:5001", "http://localhost:5000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                


                });
            });

            // Register application services
            builder.Services.AddScoped<ElevatorService>();
            builder.Services.AddScoped<BuildingService>();
            builder.Services.AddHostedService<ElevatorSimulationService>();

            // Dapper services
            builder.Services.AddSingleton<DapperContext>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // EF Core
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Elevator call services
            builder.Services.AddScoped<IElevatorCallService, ElevatorCallService>();
            builder.Services.AddScoped<IElevatorCallAssignmentService, ElevatorCallAssignmentService>();

            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseRouting();

            // Use CORS
            app.UseCors("CORSPolicy");

            // Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<ElevatorHub>("/elevatorHub");

            app.Run();
        }
    }
}
