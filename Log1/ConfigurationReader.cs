using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Log1
{
    public interface IConfigurationReader
    {
        IReadOnlyList<JsonNode> ReadConfiguration(string path);
    }

    public class ConfigurationReader : IConfigurationReader
    {
        private readonly IConfiguration configuration;

        public ConfigurationReader(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IReadOnlyList<JsonNode> ReadConfiguration(string path)
        {
            var configValues = configuration.GetSection("Log1:" + path).Get<string[]>();
            if (configValues is null)
            {
                var configValue = configuration.GetValue<string>("Log1:" + path);
                if (!(configValue is null))
                {
                    configValues = new[]
                    {
                        configValue
                    };
                }
            }

            return configValues is null ? new List<JsonNode>() : configValues.Select(s => {
                try
                {
                    return JsonNode.Parse(s);
                }
                catch
                {
                    return null;
                }
            })
            .Where(w => !(w is null))
            .ToList();
        }
    }
}
