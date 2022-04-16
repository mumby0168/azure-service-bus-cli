using Asb.Cli.Services;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Commands;

public static partial class CliCommands
{
    public static async Task PeekTopicAsync(
        string instance,
        string topic,
        string subscription,
        bool dlq,
        int? max,
        string? file,
        IConfigService configService)
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
    }

    public static async Task ReadTopicAsync(string instance,
        string topic,
        string subscription,
        bool dlq,
        int? max,
        string? file,
        int? timeout,
        bool clear,
        IConfigService configService)
    {
        timeout ??= 30;
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

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout.Value));

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

        if (!clear)
        {
            Console.WriteLine($"Processing {rawMessages.Count} message(s)");
            await ProcessMessagesAsync(rawMessages, file, configService);
        }

        Console.WriteLine($"Completing {rawMessages.Count} message(s)");

        await Task.WhenAll(rawMessages.Select(x => r.CompleteMessageAsync(x)));

        Console.WriteLine($"Completed {rawMessages.Count} message(s)");
    }
}