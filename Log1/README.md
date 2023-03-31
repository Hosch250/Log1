# Log1

Log1 is a utility that enables easily toggling logging input and output parameters.

## Usage

First, create your type and implement it normally, but add the `Log1` attribute and `virtual` keyword to the function you wish to enable logging on. Note that you can override the default log level of `Debug` in the attribute.
```
namespace MyNamespace;

public record MyType(int Arg);

public class MyService
{
    [Log1]
    public virtual void DoSomething(int a, List<int> b, MyType c, int d)
    {

    }
}
```

Next, register an `IConfigurationReader` instance with your dependency injection system; a default version reading from the `IConfiguration` source is provided for your convenience. Also, register your services with the interceptor implementation provided by the source generator:
```
services.AddSingleton<Log1.IConfigurationReader, Log1.ConfigurationReader>();
services.AddSingleton<MyNamespace.MyService, MyNamespace.Generated.MyService>();
```

Now you are all set up, and when you run your code, a message containing the function name, timestamp of the call, and arguments will be logged when you call the function, and the function name, timestamp of the response, and response value will be logged when the function returns.

To enable the logs, add JSON patterns for your code to match against to your configuration file (normally `appSettings.json`). These will be placed under the `Log1` section, and will be matched to the called function by the fully qualified name:
```
{
    "Log1": {
        "MyNamespace.MyService.DoSomething": "{ "a": 1, "b": [ 1, 2 ], "c": { "Arg": 1 } }"
    }
}
```

These patterns will be matched explicitly against the arguments passed into the function, and no logs will be created if the arguments do not match. Note that any values left out will not be compared against passed in args, whether at the root level or as a nested property.

Note that no configuration means that no logs are created to reduce the number of logs produced. To always enable logging for a function, set the configuration to `{}`. You may also need to enable debug logs for your values to displayed or saved; note in the example below I explicitly set the common hosts to allow only `Information` or higher level logs to avoid spamming useless values.

Arrays work as well as individual patterns. If an array of patterns is provided, each pattern will be checked and the logs be created if any of the patterns match the provided arguments:
```
{
    "Log1": {
        "MyNamespace.MyService.DoSomething": [
            "{ \"a\": 1 }",
            "{ \"a\": 2 }"
        ]
    },
    "Logging": {
        "LogLevel": {
            "Default": "Debug",
            "Microsoft.Hosting.Lifetime": "Information",
            "Microsoft.Extensions.Hosting.Internal.Host": "Information"
        }
    }
}
```

Because the project is designed to allow turning focused logs on and off quickly, it might not make sense to put your configuration in source control. Putting it in an environment variable is one option, but that will likely still require a deploy to refresh the settings the app is using. This is the reason the `IConfigurationReader` interface is exposed: advanced users might want to connect this to an API such as `LaunchDarkly` and toggle their logs on and off as needed.

If you need to turn off all logging for a particular function, just set an argument that does not exist as the required pattern:
```
{
    "Log1": {
        "MyNamespace.MyService.DoSomething": "{ "x": 1 }"
    }
}
```

## The Meatgrinder

How does this work? A source generator ties into the C# compiler and evaluates your code. When it detects the `LogLevel` attribute, it adds a new file to the compiled project that wraps the function call. This file can be found by adding `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to your project's `PropertyGroup` and checking the `obj/Debug/<runtime-version>/generated` folder. An example generated file should look something like:
```
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Log1;

namespace Generated;

public class Log1_MyService1Interceptor : MyService1
{
    private readonly ILogger<Log1_MyService1Interceptor> log1_logger;
    private readonly IConfigurationReader log1_configurationReader;

    public Log1_MyService1Interceptor(ILogger<Log1_MyService1Interceptor> log1_logger, IConfigurationReader log1_configurationReader)
        : base()
    {
        this.log1_logger = log1_logger;
        this.log1_configurationReader = log1_configurationReader;
    }

    public override void DoSomething(int a, List<int> b, MyType c, int d)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(a)] = a,
            [nameof(b)] = b,
            [nameof(c)] = c,
            [nameof(d)] = d,
        };

        var config = log1_configurationReader.ReadConfiguration("MyService1.DoSomething");
        var logConditionsMet = config.Matches(parameters);

        if (logConditionsMet)
        {
            // todo: read log level from attribute when generating this call
            log1_logger.LogCall(Microsoft.Extensions.Logging.LogLevel.Information, parameters);
        }

        base.DoSomething(a, b, c, d);

        if (logConditionsMet)
        {
            log1_logger.LogReturn(Microsoft.Extensions.Logging.LogLevel.Information);
        }

        
    }
}
```

The namespace is based on the namespace of your type; if your type is in `MyProject.Nested`, the generated type will live in `MyProject.Nested.Generated`. If your type is in the global namespace, it will place the file in the `Generated` namespace.