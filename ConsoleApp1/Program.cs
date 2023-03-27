using Log1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public interface IDependency { }
public class Dependency : IDependency { }

public class MyService
{
    private readonly IDependency dependency;

    public MyService(IDependency dependency)
    {
        this.dependency = dependency;
    }


    // expected outcome: `Method DoSomething was called at <datetime>`
    [Log1]
    public virtual void DoSomething()
    {

    }

    // expected outcome: `Method DoSomethingElse was called at <datetime> with parameters:`
    // { a = true, b = 3, c = [ 1, 2, 3 ] }
    [Log1(LogLevel = LogLevel.Warning)]
    public virtual int DoSomethingElse(bool a, int b, List<int> c)
    {
        return 4;
    }

    // expected outcome: nothing
    public virtual void DisableLogging()
    {

    }

    // expected outcome: log if a = 1 (per appSettings config)
    [Log1(LogLevel = LogLevel.Critical)]
    public virtual void ConditionalLogging(int a)
    {

    }

    // expected outcome: log if a = 1 (per appSettings config)
    [Log1(LogLevel = LogLevel.Critical)]
    public virtual List<int> ListReturnType(List<int> a)
    {
        return a;
    }
}

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
            //services.AddSingleton<MyService, Log1_MyServiceInterceptor>();

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