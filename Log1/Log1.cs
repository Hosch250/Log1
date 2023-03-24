using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Log1;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class Log1Attribute : Attribute
{
    public bool Disable { get; set; } = false;
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;
}

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
    [Log1(Disable = true)]
    public virtual void DisableLogging()
    {

    }

    // expected outcome: log if a = 1 (per appSettings config)
    [Log1(LogLevel = LogLevel.Critical)]
    public virtual void ConditionalLogging(int a)
    {

    }
}

// todo: auto-generate this stuff
public class Log1_MyServiceInterceptor : MyService
{
    private const string CallMessage = "Method {Name} was called at {DateTime} with {Parameters}";
    private const string ReturnsMessage = "Method {Name} returned at {DateTime} with {Value}";
    private const string VoidReturnsMessage = "Method {Name} returned at {DateTime}";

    private readonly ILogger<Log1_MyServiceInterceptor> log1Logger;
    private readonly IConfiguration config;

    public Log1_MyServiceInterceptor(ILogger<Log1_MyServiceInterceptor> log1Logger, IConfiguration config, IDependency dependency) : base(dependency)
    {
        this.log1Logger = log1Logger;
        this.config = config;
    }

    public override void DoSomething()
    {
        var parameters = new Dictionary<string, object>();

        // todo: read log level from attribute when generating this type
        LogCall(LogLevel.Information, parameters);
        base.DoSomething();
        LogReturn(LogLevel.Information);
    }

    // expected outcome: `Method DoSomethingElse was called at <datetime> with parameters:`
    // { a = true, b = 3, c = [ 1, 2, 3 ] }
    public override int DoSomethingElse(bool a, int b, List<int> c)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(a)] = a,
            [nameof(b)] = b,
            [nameof(c)] = c
        };

        // todo: read log level from attribute when generating this type
        LogCall(LogLevel.Warning, parameters);
        var response = base.DoSomethingElse(a, b, c);
        LogReturn(LogLevel.Warning, response);

        return response;
    }

    // expected outcome: nothing
    public override void DisableLogging()
    {
        base.DisableLogging();
    }

    public override void ConditionalLogging(int a)
    {
        var method = typeof(MyService).GetMember(nameof(ConditionalLogging))[0];
        var type = method.DeclaringType;
        var fullName = string.Format("{0}.{1}", type.FullName, method.Name);

        var aParamConfig = config.GetSection("Log1").GetSection(fullName).GetValue<string>(nameof(a));

        var parameters = new Dictionary<string, object>
        {
            [nameof(a)] = a,
        };

        // todo: make this work with objects and recursively check only configured fields
        // todo: make this work with multiple params (possibly loop over `parameters`?)
        if (aParamConfig == a.ToString())
        {
            // todo: read log level from attribute when generating this type
            LogCall(LogLevel.Warning, parameters);
        }
        base.ConditionalLogging(a);

        if (aParamConfig == a.ToString())
        {
            LogReturn(LogLevel.Warning);
        }
    }

    private void LogCall(LogLevel logLevel, Dictionary<string, object> parameters, [CallerMemberName] string caller = "")
    {
        log1Logger.Log(logLevel, CallMessage, caller, DateTime.UtcNow, JsonSerializer.Serialize(parameters));
    }

    private void LogReturn(LogLevel logLevel, object returns, [CallerMemberName] string caller = "")
    {
        log1Logger.Log(logLevel, ReturnsMessage, caller, DateTime.UtcNow, JsonSerializer.Serialize(returns));
    }

    private void LogReturn(LogLevel logLevel, [CallerMemberName] string caller = "")
    {
        log1Logger.Log(logLevel, VoidReturnsMessage, caller, DateTime.UtcNow);
    }
}