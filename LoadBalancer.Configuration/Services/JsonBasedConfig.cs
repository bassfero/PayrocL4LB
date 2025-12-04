using System.Text.Json;
using LoadBalancer.Configuration.DTO;
using LoadBalancer.Domain.Interfaces;
using LoadBalancer.Domain.Models;
using LoadBalancer.Domain.Shared;

namespace LoadBalancer.Configuration.Services;

public class JsonBasedConfig : IConfiguration
{
    private readonly string _jsonContent;
    private ConfigDto _config;

    public JsonBasedConfig(string jsonContent)
    {
        _jsonContent = jsonContent ?? throw new ArgumentNullException(nameof(jsonContent));
    }

    private ConfigDto LoadConfig()
    {
        if (_config != null)
            return _config;
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var config = JsonSerializer.Deserialize<ConfigDto>(_jsonContent, options);
            if (config == null)
            {
                throw new InvalidOperationException("Configuration file is empty or malformed.");
            }
            
            _config =  config;
            return config;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing configuration: {ex.Message}");
            throw new InvalidOperationException("Failed to parse JSON configuration.", ex);
        }
    }

    public IReadOnlyList<LBNode> GetInitialListOfNodes()
    {
        var config = LoadConfig();
        if (config.Nodes == null || !config.Nodes.Any())
        {
            return new List<LBNode>().AsReadOnly();
        }
        
        return config.Nodes.Select(n => n.ToLBNode()).ToList().AsReadOnly();
    }

    public LoadBalancingStrategyType GetStrategyType()
    {
        var config = LoadConfig();
        if (Enum.TryParse(config.Strategy, true, out LoadBalancingStrategyType type))
        {
            return type;
        }
        
        // Fallback
        Console.WriteLine($"Warning: Invalid strategy '{config.Strategy}' found. Defaulting to RoundRobin.");
        return LoadBalancingStrategyType.RoundRobin;
    }

    public int LBListeningPort()
    {
        var config = LoadConfig();
        return config.ListeningPort ?? 8080;
    }

    public int HealthCheckInterval()
    {
        var config = LoadConfig();
        return config.HealthCheckInterval ?? 30000;
    }
}