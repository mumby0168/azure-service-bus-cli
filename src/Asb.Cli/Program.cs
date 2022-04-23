using Asb.Cli.Commands;
using Asb.Cli.Options;
using Asb.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder(
    args,
    options => { options.EnableShellCompletionSupport = true; });

builder.Configuration.AddJsonFile(
    Path.Combine(
        DefaultConfigService.ConfigDirectory,
        "appsettings.json"),
    false);

builder.Services
    .AddOptions<CliOptions>()
    .Configure<IConfiguration>((options, config) =>
        config.GetSection(nameof(CliOptions)).Bind(options));

builder.Services
    .AddSingleton<IConfigService, DefaultConfigService>();

var app = builder.Build();

app.AddSubCommand("topic", commandsBuilder =>
{
    commandsBuilder
        .AddCommand("peek", CliCommands.PeekTopicAsync)
        .WithAliases("p");

    commandsBuilder
        .AddCommand("read", CliCommands.ReadTopicAsync)
        .WithAliases("r");

    commandsBuilder
        .AddCommand("retry", CliCommands.ReadTopicAsync)
        .WithAliases("rt");
}).WithAliases("t");

app.AddSubCommand("queue", commandsBuilder =>
{
    commandsBuilder
        .AddCommand("peek", CliCommands.PeekQueueAsync)
        .WithAliases("p");

    commandsBuilder
        .AddCommand("read", CliCommands.ReadQueueAsync)
        .WithAliases("r");
}).WithAliases("q");

app.AddSubCommand("config", commandsBuilder =>
{
    commandsBuilder.AddCommand("save", CliCommands.SaveConnectionStringAsync);
    commandsBuilder.AddCommand("list", CliCommands.ListConfig);
}).WithAliases("c");

app.Run();