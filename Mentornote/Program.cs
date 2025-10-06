#nullable disable
using Mentornote.Controllers;
using Mentornote.Data;
using Mentornote.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;

namespace Mentornote
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            
            Console.WriteLine($"JWT Key: {jwtSettings["Key"]}");
            Console.WriteLine($"JWT Issuer: {jwtSettings["Issuer"]}");
            Console.WriteLine($"JWT Audience: {jwtSettings["Audience"]}");

            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
            if (string.IsNullOrEmpty(jwtSettings["Key"]))
            {
                throw new Exception("JWT Key is missing in appsettings.json!");
            }

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient();
            builder.Services.AddRazorPages();
            builder.Services.AddSession();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<FlashcardService>();
            builder.Services.AddScoped<FlashCardsController>();
            builder.Services.AddScoped<AuthController>();
            builder.Services.AddScoped<CardsServices>();
            builder.Services.AddScoped<NotesSummaryService>();
            builder.Services.AddScoped<TutorServices>();
            builder.Services.AddScoped<TestServices>();
            builder.Services.AddScoped<YouTubeVideoService>();
            builder.Services.AddScoped<SpeechCaptureServices>();
            builder.Services.AddScoped<Helpers>();
            builder.Services.AddHttpContextAccessor();
 /*           builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });*/
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
            });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        
             var app = builder.Build();


            app.UseDeveloperExceptionPage();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.MapHub<ProcessingHub>("/processingHub");

            app.UseRouting();

            // Authentication & Authorization MUST be after routing but before endpoints
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapRazorPages();
            app.MapControllers();

            app.Run();
        }
    }
}
