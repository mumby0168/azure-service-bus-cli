using System.Text.Json;

namespace Asb.Cli.Services;

public interface IConfigService
{
    string? TryGetConnectionString(string key);

    ValueTask SaveConnectionStringAsync(string key, string connectionString);
    
    Dictionary<string, string> ConnectionStrings { get; }

    JsonSerializerOptions SerializerOptions { get; }
}