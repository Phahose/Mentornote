#nullable disable
using Mentornote.Backend;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Collections;
using System.Text;
namespace Mentornote.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ENV DUMP:");
            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                if (e.Key.ToString()!.Contains("ConnectionStrings"))
                {
                    Console.WriteLine($"{e.Key} = {e.Value}");
                }
                    
            }


            var builder = WebApplication.CreateBuilder(args);

            // Stripe configuration binding
            builder.Services.Configure<StripeSettings>(
                builder.Configuration.GetSection("Stripe"));

            // Make StripeSettings injectable
            builder.Services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<StripeSettings>>().Value);


            // Register your service with the key
            builder.Services.AddSingleton<Transcribe>();
            builder.Services.AddSingleton<ConversationMemory>();  
            builder.Services.AddSingleton<RagService>();
            builder.Services.AddSingleton<AudioListener>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<DBServices>();
            builder.Services.AddSingleton<FileServices>();
            builder.Services.AddSingleton<GeminiServices>();


            //Add controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var cs = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(cs))
            {

                Console.WriteLine(
                        builder.Configuration["ConnectionStrings:DefaultConnection"] ?? "NULL_FROM_CONFIG"
                    );

                throw new InvalidOperationException($"DefaultConnection is missing. This is what is there {cs}");
            }


            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };
                });


            var app = builder.Build();

            //Swagger setup
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapGet("/health", () => Results.Ok("Healthy"));
            app.MapGet("/", () => Results.Ok("MentorNote API is running"));
            app.Run();
        }
    }

 
}
