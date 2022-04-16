using Asb.Cli.Options;
using Asb.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();

builder.Configuration.AddJsonFile("appsettings.json", false);

builder.Services
    .AddOptions<CliOptions>()
    .Configure<IConfiguration>((options, config) =>
        config.GetSection(nameof(CliOptions)).Bind(options));

builder.Services
    .AddSingleton<IConfigService, DefaultConfigService>();

var app = builder.Build();

app.AddSubCommand("topics", commandsBuilder => { });

app.AddSubCommand("config", commandsBuilder =>
{
    commandsBuilder.AddCommand(
        "save",
        async (string key, string value, IConfigService configService) =>
        {
            await configService.SaveConnectionStringAsync(key, value);
            Console.WriteLine("Successfully saved new connection string");
        });
    
    commandsBuilder.AddCommand(
        "list",
        (IConfigService configService) =>
        {
            Console.WriteLine(configService.ConnectionStrings.Count);
            foreach (var (key, value) in configService.ConnectionStrings)
            {
                Console.WriteLine($"({key} => {value})");
            }
        });
});

app.Run();