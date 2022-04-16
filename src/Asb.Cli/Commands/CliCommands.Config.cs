using Asb.Cli.Services;

namespace Asb.Cli.Commands;

public static partial class CliCommands
{
    public static async Task SaveConnectionStringAsync(
        string key,
        string value,
        IConfigService configService)
    {
        await configService.SaveConnectionStringAsync(key, value);
        Console.WriteLine("Successfully saved new connection string");
    }

    public static void ListConfig(IConfigService configService)
    {
        Console.WriteLine(configService.ConnectionStrings.Count);
        foreach (var (key, value) in configService.ConnectionStrings)
        {
            Console.WriteLine($"({key} => {value})");
        }
    }
}