#nullable disable
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Mentornote.Backend;


namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        //public static IHost AppHost { get; private set; }

        public App()
        {
            //AppHost = Host.CreateDefaultBuilder()
            //    .ConfigureServices((context, services) =>
            //    {
            //        services.AddSingleton<ConversationMemory>();
            //        services.AddSingleton<Transcribe>();
            //        services.AddSingleton<GeminiServices>();
            //        services.AddTransient<Overlay>();
            //        services.AddTransient<MainWindow>();
            //    })
            //    .Build();
        }
    }

}
