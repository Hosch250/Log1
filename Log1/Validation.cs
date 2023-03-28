using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log1
{
    public static class Validation
    {
        public static bool Matches(this JsonNode expected, Dictionary<string, object> args)
        {
            if (expected is null)
            {
                return true;
            }

            var actual = JsonNode.Parse(JsonSerializer.Serialize(args));

            return JsonComparison.CompareJson(expected, actual);
        }
    }
}