using Asb.Cli.Services;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Commands;

public static partial class CliCommands
{
    public static async Task PeekTopicAsync(
        [Option(new[] {'i'}, Description = HelpDescriptions.Instance)]
        string instance,
        [Option(new[] {'t'}, Description = HelpDescriptions.Topic)]
        string topic,
        [Option(new[] {'s'}, Description = HelpDescriptions.Subscription)]
        string subscription,
        [Option(Description = HelpDescriptions.DeadLetterQueue)]
        bool dlq,
        [Option(Description = HelpDescriptions.Max)]
        int? max,
        [Option(Description = HelpDescriptions.File)]
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

    public static async Task ReadTopicAsync(
        [Option(new[] {'i'}, Description = HelpDescriptions.Instance)]
        string instance,
        [Option(new[] {'t'}, Description = HelpDescriptions.Topic)]
        string topic,
        [Option(new[] {'s'}, Description = HelpDescriptions.Subscription)]
        string subscription,
        [Option(Description = HelpDescriptions.DeadLetterQueue)]
        bool dlq,
        [Option(Description = HelpDescriptions.Max)]
        int? max,
        [Option(Description = HelpDescriptions.File)]
        string? file,
        [Option(Description = HelpDescriptions.Timeout)]
        int? timeout,
        [Option(Description = HelpDescriptions.Clear)]
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

    public static async Task RetryTopicMessagesAsync(
        [Option(new[] {'i'}, Description = HelpDescriptions.Instance)]
        string instance,
        [Option(new[] {'t'}, Description = HelpDescriptions.Topic)]
        string topic,
        [Option(new[] {'s'}, Description = HelpDescriptions.Subscription)]
        string subscription,
        [Option(Description = HelpDescriptions.Max)]
        int? max,
        [Option(Description = HelpDescriptions.File)]
        string? file,
        [Option(Description = HelpDescriptions.Timeout)]
        int? timeout,
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

        if (!subscription.EndsWith("$deadLetterQueue"))
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
        Console.WriteLine($"Retrying {rawMessages.Count} message(s)");

        await using var sender = sb.CreateSender(topic);

        Console.WriteLine($"Processing {rawMessages.Count} message(s)");
        await ProcessMessagesAsync(rawMessages, file, configService);

        Console.WriteLine($"Completing {rawMessages.Count} message(s)");

        await Task.WhenAll(rawMessages.Select(x => 
            r.CompleteMessageAsync(x, cts.Token)));

        Console.WriteLine($"Completed {rawMessages.Count} message(s)");
    }
}