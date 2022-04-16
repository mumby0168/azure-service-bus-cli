using System.Text.Json;
using Asb.Cli.Models;
using Asb.Cli.Services;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Commands;

public static partial class CliCommands 
{
    private static async Task ProcessMessagesAsync(
        IEnumerable<ServiceBusReceivedMessage> serviceBusReceivedMessages, 
        string? s, 
        IConfigService configService)
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
                JsonSerializer.Serialize(messages, configService.SerializerOptions));

            Console.WriteLine($"Written messages to file {s}");
        }
        else
        {
            foreach (var message in messages)
            {
                Console.WriteLine(JsonSerializer.Serialize(message.Message, configService.SerializerOptions));
            }
        }
    }
}