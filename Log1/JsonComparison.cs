using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;

namespace Log1
{
    internal static class JsonComparison
    {
        public static bool CompareJson(JsonNode expected, JsonNode actual)
        {
            if (expected is null || actual is null)
            {
                Console.WriteLine("Comparing to 'null' value");
                return false;
            }

            if (expected is JsonArray expectedArray && actual is JsonArray actualArray)
            {
                return CompareJson(expectedArray, actualArray);
            }
            else if (expected is JsonObject expectedObject && actual is JsonObject actualObject)
            {
                return CompareJson(expectedObject, actualObject);
            }
            else if (expected is JsonValue &&
                actual is JsonValue &&
                expected.GetValue<object>() is JsonElement expectedElement &&
                actual.GetValue<object>() is JsonElement actualElement)
            {
                return CompareJson(expectedElement, actualElement);
            }
            else
            {
                throw new NotImplementedException($"Comparison not implemented for type {expected.GetType().FullName}");
            }
        }

        public static bool CompareJson(JsonObject expected, JsonObject actual)
        {
            foreach (var item in expected)
            {
                if (!actual.ContainsKey(item.Key))
                {
                    Console.WriteLine($"Missing key '{item.Key}'");
                    return false;
                }

                var expectedNode = expected[item.Key];
                var actualNode = actual[item.Key];

                // a json `null` literal gets returned as a `null` C# object
                if (expectedNode is null && actualNode is null)
                {
                    continue;
                }
                else if (expectedNode is null || actualNode is null)
                {
                    return false;
                }

                if (expectedNode.GetType() != actualNode.GetType())
                {
                    Console.WriteLine("Type mismatch");
                    return false;
                }


                if (!CompareJson(expectedNode, actualNode))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CompareJson(JsonArray expected, JsonArray actual)
        {
            return expected.All(a => actual.Any(b => CompareJson(a, b)));
        }

        public static bool CompareJson(JsonElement expected, JsonElement actual)
        {
            return expected.ValueKind == actual.ValueKind && expected.GetRawText() == actual.GetRawText();
        }
    }
}