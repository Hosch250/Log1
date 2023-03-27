using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Log1
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Log1Attribute : Attribute
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
    }

    public static class Log1
    {
        private const string CallMessage = "Method {Name} was called at {DateTime} with {Parameters}";
        private const string ReturnsMessage = "Method {Name} returned at {DateTime} with {Value}";
        private const string VoidReturnsMessage = "Method {Name} returned at {DateTime}";

        public static void LogCall(this ILogger logger, LogLevel logLevel, Dictionary<string, object> parameters, [CallerMemberName] string caller = "")
        {
            logger.Log(logLevel, CallMessage, caller, DateTime.UtcNow, JsonSerializer.Serialize(parameters));
        }

        public static void LogReturn(this ILogger logger, LogLevel logLevel, object returns, [CallerMemberName] string caller = "")
        {
            logger.Log(logLevel, ReturnsMessage, caller, DateTime.UtcNow, JsonSerializer.Serialize(returns));
        }

        public static void LogReturn(this ILogger logger, LogLevel logLevel, [CallerMemberName] string caller = "")
        {
            logger.Log(logLevel, VoidReturnsMessage, caller, DateTime.UtcNow);
        }
    }
}