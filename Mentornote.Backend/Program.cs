#nullable disable
using System.Text;
using Mentornote.Backend;
using Microsoft.Extensions.Options;

namespace Mentornote.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Register your service with the key
            builder.Services.AddSingleton<Transcribe>();
            builder.Services.AddSingleton<GeminiServices>();
            builder.Services.AddSingleton<ConversationMemory>();  

            //Add controllers and Swagger (same as before)
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            //Swagger setup
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }

 
}
