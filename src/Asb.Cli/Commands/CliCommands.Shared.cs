using System.Text.Json;
using Asb.Cli.Models;
using Asb.Cli.Services;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Commands;

public static partial class CliCommands 
{
    private static async Task ProcessMessagesAsync(
        IEnumerable<ServiceBusReceivedMessage> serviceBusReceivedMessages, 
        string? filePath, 
        IConfigService configService)
    {
        var messages = serviceBusReceivedMessages.Select(x => new AsbMessage(x)).ToList();

        if (messages is {Count: 0})
        {
            Console.WriteLine("There are no messages to peek");
            return;
        }

        if (filePath is not null)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(messages, configService.SerializerOptions));

            Console.WriteLine($"Written messages to file {filePath}");
        }
        else
        {
            foreach (var message in messages)
            {
                Console.WriteLine(JsonSerializer.Serialize(message.Message, configService.SerializerOptions));
            }
        }
    }

    private static class HelpDescriptions
    {
        public const string Instance = "The key used to get the azure service bus connection string.";

        public const string Queue = "The queue in which to use in this operation ('~' will be replaced with `/`).";

        public const string DeadLetterQueue = "Whether or not to use the dead letter queue in this operation.";

        public const string Max = "The maximum amount of messages used in this operation";
        
        public const string File = "The relative file path to write the messages used in this operation.";
        
        public const string Timeout = "The time in seconds in which this operation will run before being cancelled.";
        
        public const string Clear = "Whether or not the message entity is being cleared down.";
        
        public const string Topic = "The topic to be used in this operation ('~' will be replaced with `/`).";
        
        public const string Subscription = "The subscription name to use in this operation.";

    }
}