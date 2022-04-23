using System.Reflection;
using System.Text.Json;
using Asb.Cli.Options;
using Microsoft.Extensions.Options;

namespace Asb.Cli.Services;

public class DefaultConfigService : IConfigService
{
    public static readonly string ConfigDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    private readonly CliOptions _cliOptions;

    public DefaultConfigService(IOptions<CliOptions> optionsMonitor) =>
        _cliOptions = optionsMonitor.Value;

    public string? TryGetConnectionString(string key) => 
        _cliOptions.Instances.TryGetValue(key, out var connectionString) 
            ? connectionString 
            : null;

    public async ValueTask SaveConnectionStringAsync(string key, string connectionString)
    {
        connectionString = connectionString.Trim();
        
        if (_cliOptions.Instances.ContainsKey(key))
        {
            _cliOptions.Instances[key] = connectionString;
        }
        else
        {
            _cliOptions.Instances.Add(key, connectionString);
        }

        var json = JsonSerializer.Serialize(_cliOptions.WithWrapper(), SerializerOptions);

        await File.WriteAllTextAsync(
            Path.Combine(
                ConfigDirectory,
                "appsettings.json"),
            json);
    }

    public Dictionary<string, string> ConnectionStrings =>
        _cliOptions.Instances;

    public JsonSerializerOptions SerializerOptions =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
}