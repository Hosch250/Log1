using Log1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var workerInstance = host.Services.GetRequiredService<Worker>();
        await workerInstance.Execute();
        await host.StopAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(a =>
        {
            a.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
        })
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IDependency, Dependency>();
            services.AddMyServiceAsSingleton();

            services.AddTransient<Worker>();
        })
        .ConfigureLogging((_, logging) =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options => options.IncludeScopes = true);
        });
}

public class Worker
{
    private readonly MyService service;

    public Worker(MyService service)
    {
        this.service = service;
    }

    public Task Execute()
    {
        service.DoSomething();
        service.DoSomethingElse(true, 3, new() { 1, 2, 3 });
        service.DisableLogging();
        service.ConditionalLogging(1);
        service.ConditionalLogging(2);

        return Task.CompletedTask;
    }
}