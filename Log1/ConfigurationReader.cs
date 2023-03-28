using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace Log1
{
    public interface IConfigurationReader
    {
        JsonNode ReadConfiguration(string path);
    }

    public class ConfigurationReader : IConfigurationReader
    {
        private readonly IConfiguration configuration;

        public ConfigurationReader(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public JsonNode ReadConfiguration(string path)
        {
            var configValue = configuration.GetValue<string>("Log1:" + path);
            return configValue is null ? null : JsonNode.Parse(configValue);
        }
    }
}
