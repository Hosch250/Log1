using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log1;

public static class JsonComparison
{
    public static bool CompareJson(JsonNode node1, JsonNode node2)
    {
        if (node1 is null || node2 is null)
        {
            Console.WriteLine("Comparing to 'null' value");
            return false;
        }

        if (node1 is JsonArray expectedArray && node2 is JsonArray actualArray)
        {
            return CompareJson(expectedArray, actualArray);
        }
        else if (node1 is JsonObject expectedObject && node2 is JsonObject actualObject)
        {
            return CompareJson(expectedObject, actualObject);
        }
        else if (node1 is JsonValue &&
            node2 is JsonValue &&
            node1.GetValue<object>() is JsonElement expectedElement &&
            node2.GetValue<object>() is JsonElement actualElement)
        {
            return CompareJson(expectedElement, actualElement);
        }
        else
        {
            throw new NotImplementedException($"Comparison not implemented for type {node1.GetType().FullName}");
        }
    }

    public static bool CompareJson(JsonObject obj1, JsonObject obj2)
    {
        foreach (var item in obj1)
        {
            if (!obj2.ContainsKey(item.Key))
            {
                Console.WriteLine($"Missing key '{item.Key}'");
                return false;
            }

            var expected = obj1[item.Key];
            var actual = obj2[item.Key];

            // a json `null` literal gets returned as a `null` C# object
            if (actual is null && expected is null)
            {
                continue;
            }

            if (actual is null)
            {
                Console.WriteLine("Actual value not found");
                return false;
            }

            if (expected!.GetType() != actual.GetType())
            {
                Console.WriteLine("Type mismatch");
                return false;
            }


            if (!CompareJson(expected, actual))
            {
                return false;
            }
        }

        return true;
    }

    public static bool CompareJson(JsonArray node1, JsonArray node2)
    {
        return node1.All(a => node2.Any(b => CompareJson(a!, b!)));
    }

    public static bool CompareJson(JsonElement node1, JsonElement node2)
    {
        return node1.ValueKind == node2.ValueKind && node1.GetRawText() == node2.GetRawText();
    }
}