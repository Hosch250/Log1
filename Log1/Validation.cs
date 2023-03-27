using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log1
{
    public static class Validation
    {
        private static string GetMemberConfig(this IConfiguration configuration, Type type, string memberName)
        {
            var fullName = string.Format("{0}.{1}", type.FullName, memberName);
            return configuration.GetValue<string>($"Log1:{fullName}");
        }

        public static bool EvaluateConfiguration(this IConfiguration configuration, Type type, string memberName, Dictionary<string, object> args)
        {
            var configValue = GetMemberConfig(configuration, type, memberName);
            if (string.IsNullOrWhiteSpace(configValue))
            {
                return true;
            }

            var expected = JsonNode.Parse(configValue);
            var actual = JsonNode.Parse(JsonSerializer.Serialize(args));

            return JsonComparison.CompareJson(expected, actual);
        }
    }
}