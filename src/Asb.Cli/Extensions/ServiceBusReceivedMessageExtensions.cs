using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Extensions;

public static class ServiceBusReceivedMessageExtensions
{
    public static JsonDocument GetJson(this ServiceBusReceivedMessage message) => 
        JsonDocument.Parse(message.Body);
}