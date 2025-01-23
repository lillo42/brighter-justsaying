using System.Diagnostics;
using System.Text.Json.Serialization;

using Paramore.Brighter;

namespace Brighter.Interop.JustSaying;

/// <summary>
/// The interface for JustSayingMessage
/// </summary>
public interface IJustSayingMessage : IRequest
{
    /// <summary>
    /// The time stamp when the message was created.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The raising component.
    /// </summary>
    public string? RaisingComponent { get; set; }

    /// <summary>
    /// The curret version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The source IP
    /// </summary>
    /// <remarks>
    /// It's used by Just Saying
    /// </remarks>
    public string? SourceIp { get; set; }

    /// <summary>
    /// The message tenant.
    /// </summary>
    public string Tenant { get; set; }

    /// <summary>
    /// The conversation id.
    /// </summary>
    public string? Conversation { get; set; }
}

/// <summary>
/// The base JustSayingMessage
/// </summary>
public abstract class JustSayingMessage : IJustSayingMessage
{
    /// <summary>
    /// Initialize the <see cref="JustSayingMessage"/>
    /// </summary>
    public JustSayingMessage()
    {
    }

    /// <summary>
    /// Initialize the <see cref="JustSayingMessage"/>
    /// </summary>
    /// <param name="id">The message id.</param>
    public JustSayingMessage(Guid id)
    {
        Id = id;
    }

    /// <inheritdoc /> 
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc /> 
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [NJsonSchema.Annotations.JsonSchemaIgnore]
    public Activity? Span { get; set; }

    /// <inheritdoc /> 
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <inheritdoc /> 
    public string? RaisingComponent { get; set; }

    /// <inheritdoc /> 
    public string? Version { get; set; }
    
    /// <inheritdoc /> 
    public string? SourceIp { get; set; }

    /// <inheritdoc /> 
    public string Tenant { get; set; } = "all";

    /// <inheritdoc /> 
    public string? Conversation { get; set; }
}