using System.Text.Json;
using Asb.Cli.Extensions;
using Asb.Cli.Models;
using Asb.Cli.Options;
using Asb.Cli.Services;
using Azure.Messaging.ServiceBus;
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

app.AddSubCommand("topic", commandsBuilder =>
{
    commandsBuilder.AddCommand("peek", async (
        string instance,
        string topic,
        string subscription,
        bool dlq,
        int? max,
        string? file,
        IConfigService configService) =>
    {
        max ??= 10;
        topic = topic.Replace("~", "/");
        
        var cs = configService.TryGetConnectionString(instance);

        if (cs is null)
        {
            Console.WriteLine($"There is no connection string for key {cs}");
            return;
        }
        
        await using var sb = new ServiceBusClient(cs);

        if (dlq)
        {
            subscription = $"{subscription}/$deadLetterQueue";
        }

        await using var r = sb.CreateReceiver(topic, subscription);

        var rawMessages = await r.PeekMessagesAsync(max.Value);

        await ProcessMessagesAsync(rawMessages, file, configService);
    });
    
    commandsBuilder.AddCommand("read", async (
        string instance,
        string topic,
        string subscription,
        bool dlq,
        int? max,
        string? file,
        IConfigService configService) =>
    {
        max ??= 10;
        topic = topic.Replace("~", "/");
        
        Console.WriteLine(max.Value);
        
        var cs = configService.TryGetConnectionString(instance);

        if (cs is null)
        {
            Console.WriteLine($"There is no connection string for key {cs}");
            return;
        }
        
        await using var sb = new ServiceBusClient(cs);

        if (dlq)
        {
            subscription = $"{subscription}/$deadLetterQueue";
        }

        await using var r = sb.CreateReceiver(topic, subscription);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var rawMessages = new List<ServiceBusReceivedMessage>();

        await foreach (var m in r.ReceiveMessagesAsync(cts.Token))
        {
            if (rawMessages.Count >= max)
            {
                break;
            }
            
            rawMessages.Add(m);
        }

        Console.WriteLine($"Read {rawMessages.Count} message(s)");

        await ProcessMessagesAsync(rawMessages, file, configService);

        await Task.WhenAll(rawMessages.Select(x => r.CompleteMessageAsync(x)));
    });
});

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

async Task ProcessMessagesAsync(IReadOnlyList<ServiceBusReceivedMessage> serviceBusReceivedMessages, string? s, IConfigService configService1)
{
    var messages = serviceBusReceivedMessages.Select(x => new AsbMessage(x)).ToList();

    if (messages is {Count: 0})
    {
        Console.WriteLine("There are no messages to peek");
        return;
    }

    if (s is not null)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), s);

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(messages, configService1.SerializerOptions));

        Console.WriteLine($"Written messages to file {s}");
    }
    else
    {
        foreach (var message in messages)
        {
            Console.WriteLine(JsonSerializer.Serialize(message.Message, configService1.SerializerOptions));
        }
    }
}