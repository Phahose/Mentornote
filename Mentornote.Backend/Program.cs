#nullable disable
using Mentornote.Backend;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;



namespace Mentornote.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Stripe configuration binding
            builder.Services.Configure<StripeSettings>(
                builder.Configuration.GetSection("Stripe"));

            // Make StripeSettings injectable
            builder.Services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<StripeSettings>>().Value);

            // Set Stripe API key (GLOBAL)
            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];


            // Register your service with the key
            builder.Services.AddSingleton<Transcribe>();
            builder.Services.AddSingleton<ConversationMemory>();  
            builder.Services.AddSingleton<RagService>();
            builder.Services.AddSingleton<AudioListener>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<DBServices>();
            builder.Services.AddSingleton<FileServices>();
            builder.Services.AddSingleton<GeminiServices>();


            //Add controllers and Swagger (same as before)
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
            app.Run();
        }
    }

 
}
