using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log1
{
    public static class Validation
    {
        public static bool Matches(this IReadOnlyList<JsonNode> expected, Dictionary<string, object> args)
        {
            if (expected is null || !expected.Any())
            {
                return true;
            }

            var actual = JsonNode.Parse(JsonSerializer.Serialize(args));

            return expected.Any(a => JsonComparison.CompareJson(a, actual));
        }
    }
}