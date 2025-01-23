using System.Text.Json;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Paramore.Brighter;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Brighter.Interop.JustSaying.Mappers;

/// <summary>
/// The just saying mapper. 
/// </summary>
/// <param name="mapperConfiguration">The <see cref="JustSayingMapperConfiguration"/>.</param>
/// <typeparam name="T">The just saying message.</typeparam>
public class JustSayingMapper<T>(IOptionsSnapshot<JustSayingMapperConfiguration> mapperConfiguration)
    : IAmAMessageMapper<T>
    where T : class, IJustSayingMessage
{
    /// <summary>
    /// The <see cref="JustSayingMapperConfiguration"/>.
    /// </summary>
    protected virtual JustSayingMapperConfiguration MapperConfiguration { get; } =
        mapperConfiguration.Get(typeof(T).Name);

    /// <inheritdoc cref="IAmAMessageMapper{TRequest}.MapToMessage"/>
    public virtual Message MapToMessage(T request)
    {
        var configuration = MapperConfiguration;
        if (string.IsNullOrEmpty(request.Conversation))
        {
            request.Conversation = configuration.ConversationFactory().ToString();
        }

        request.RaisingComponent ??= configuration.ComponentName;
        request.Version ??= configuration.Version;

        var header = CreateHeader(request, configuration);
        var body = CreateBody(request, configuration);

        return new Message(header, body);
    }

    /// <summary>
    /// Create the <see cref="MessageHeader"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="configuration">The <see cref="JustSayingMapperConfiguration"/>.</param>
    /// <returns>
    /// New instance of <see cref="MessageHeader"/> based on the provided configuration and <paramref name="request"/>.
    /// </returns>
    protected virtual MessageHeader CreateHeader(T request, JustSayingMapperConfiguration configuration)
    {
        var header = new MessageHeader
        {
            Id = request.Id,
            TimeStamp = request.TimeStamp,
            ContentType = MessageBody.APPLICATION_JSON,
            CorrelationId = GetCorrelationId(request.Conversation),
            Topic = GetTopic(request, configuration.Topic, configuration.Environment),
            MessageType = request switch
            {
                ICommand => MessageType.MT_COMMAND,
                _ => MessageType.MT_EVENT, // In case we don't know the type, we are assuming it'll be an event
            },
            Bag = configuration.Bag ?? new Dictionary<string, object>()
        };
        
        header.Bag["Subject"] = typeof(T).Name;

        return header;

        static Guid GetCorrelationId(string? conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return Guid.Empty;
            }

            return Guid.TryParse(conversationId, out var id) ? id : Guid.Empty;
        }

        static string GetTopic(T request,
            string? topic,
            string environment)
        {
            if (!string.IsNullOrEmpty(topic))
            {
                return topic
                    .Replace("{Tenant}", request.Tenant.ToLower())
                    .Replace("{Environment}", environment)
                    .Replace("{Type}", typeof(T).Name.ToLower());
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Create the <see cref="MessageBody"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="configuration">The <see cref="JustSayingMapperConfiguration"/>.</param>
    /// <returns>
    /// New instance of <see cref="MessageBody"/> based on the provided configuration and <paramref name="request"/>.
    /// </returns>
    protected virtual MessageBody CreateBody(T request, JustSayingMapperConfiguration configuration)
    {
        if (configuration.NewtonsoftSerializeOptions != null)
        {
            return new MessageBody(JsonConvert.SerializeObject(request, configuration.NewtonsoftSerializeOptions));
        }

        return new MessageBody(JsonSerializer.Serialize(request, configuration.SystemTextJsonSerializeOptions));
    }

    /// <inheritdoc cref="IAmAMessageMapper{TRequest}.MapToRequest"/>
    public virtual T MapToRequest(Message message)
    {
        var req = CreateRequest(message.Body, MapperConfiguration);

        if (string.IsNullOrEmpty(req.Conversation) && message.Header.CorrelationId != Guid.Empty)
        {
            req.Conversation = message.Header.CorrelationId.ToString();
        }

        if (req.TimeStamp == default && message.Header.TimeStamp != default)
        {
            req.TimeStamp = message.Header.TimeStamp;
        }

        if (req.Id == Guid.Empty && message.Header.Id != Guid.Empty)
        {
            req.Id = message.Header.Id;
        }

        return req;
    }

    /// <summary>
    /// Create the <typeparamref name="T"/>
    /// </summary>
    /// <param name="body">The <see cref="MessageBody"/>.</param>
    /// <param name="configuration">The <see cref="JustSayingMapperConfiguration"/>.</param>
    /// <returns>
    /// New instance of <typeparamref name="T"/>.
    /// </returns>
    protected virtual T CreateRequest(MessageBody body, JustSayingMapperConfiguration configuration)
    {
        if (configuration.NewtonsoftSerializeOptions != null)
        {
            return JsonConvert.DeserializeObject<T>(body.Value, configuration.NewtonsoftSerializeOptions)!;
        }

        return JsonSerializer.Deserialize<T>(body.Bytes, configuration.SystemTextJsonSerializeOptions)!;
    }
}

/// <summary>
/// The <see cref="JustSayingMapper{T}"/> configuration
/// </summary>
public class JustSayingMapperConfiguration
{
    /// <summary>
    /// The environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// The component name.
    /// </summary>
    /// <remarks>
    /// it will be used to enrich the request.
    /// </remarks>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// The application version
    /// </summary>
    /// <remarks>
    /// it will be used to enrich the request.
    /// </remarks>
    public string? Version { get; set; }

    /// <summary>
    /// The destiny topic
    /// </summary>
    /// <remarks>
    /// This property can have a replaceable value {}, supported values
    /// - Tenant
    /// - Environment
    /// - Type
    /// </remarks>
    public string? Topic { get; set; } = "{Type}";

    /// <summary>
    /// The function to get or generate the conversation if it's not provided 
    /// </summary>
    public Func<Guid> ConversationFactory { get; set; } = Guid.NewGuid;

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>
    /// In case <see cref="SystemTextJsonSerializeOptions"/> or <see cref="NewtonsoftSerializeOptions"/>
    /// are not provided it'll use System Text Json
    /// </remarks>
    public JsonSerializerOptions? SystemTextJsonSerializeOptions { get; set; }

    /// <summary>
    /// The <see cref="JsonSerializerSettings"/>. If set it'll use Newtonsoft serializer
    /// </summary>
    /// <remarks>
    /// This property has priority over <see cref="SystemTextJsonSerializeOptions"/>.
    /// </remarks>
    public JsonSerializerSettings? NewtonsoftSerializeOptions { get; set; }
    
    /// <inheritdoc cref="MessageHeader.Bag"/>
    public Dictionary<string, object>? Bag { get; set; }
}