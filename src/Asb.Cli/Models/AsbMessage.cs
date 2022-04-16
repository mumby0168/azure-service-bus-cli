using System.Text.Json;
using Asb.Cli.Extensions;
using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace Asb.Cli.Models;

public class AsbMessage
{
    public AsbMessage()
    {
        
    }
    
    public AsbMessage(ServiceBusReceivedMessage message)
    {
        Message = message.GetJson();
        CorrelationId = message.CorrelationId;
        Subject = message.Subject;
        UserProperties = message.ApplicationProperties;
        ContentType = message.ContentType;
        DeliveryCount = message.DeliveryCount;
        DeadLetterReason = message.DeadLetterReason;
        DeadLetterSource = message.DeadLetterSource;
        DeadLetterErrorDescription = message.DeadLetterErrorDescription;
        SessionId = message.SessionId;
        EnqueuedTime = message.EnqueuedTime;
    }

    public DateTimeOffset EnqueuedTime { get; set; }

    public string SessionId { get; set; } = null!;

    public string DeadLetterErrorDescription { get; set; } = null!;

    public string DeadLetterSource { get; set; } = null!;

    public string DeadLetterReason { get; set; } = null!;

    public int DeliveryCount { get; set; }

    public string ContentType { get; set; } = null!;

    public IReadOnlyDictionary<string, object> UserProperties { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string CorrelationId { get; set; } = null!;

    public JsonDocument Message { get; set; } = null!;
}