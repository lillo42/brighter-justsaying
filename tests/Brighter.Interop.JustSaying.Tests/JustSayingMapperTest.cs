using Brighter.Interop.JustSaying.Mappers;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using NSubstitute;

using Paramore.Brighter;

using Shouldly;

namespace Brighter.Interop.JustSaying.Tests;

public class JustSayingMapperTest
{
    [Fact]
    public void MapToMessage_Using_SystemTextJson()
    {
        var version = Guid.NewGuid().ToString("N");
        var component = $"RaisingComponent{Guid.NewGuid():N}";
        var environment = $"Environment{Guid.NewGuid():N}";
        var tenant = $"Tenant{Guid.NewGuid():N}".ToLower();
        
        var opts = Substitute.For<IOptionsSnapshot<JustSayingMapperConfiguration>>();
        opts.Get(Arg.Any<string>()).Returns(new JustSayingMapperConfiguration
        {
            Version = version,
            ComponentName = component,
            Environment = environment,
            Topic = "{Tenant}-{Environment}-{Type}"
        });

        var mapper = new JustSayingMapper<SomeEvent>(opts);

        var justSaying = new SomeEvent
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Conversation = Guid.NewGuid().ToString(),
            Message = "Hello World!",
            Tenant = tenant
        };

        var message = mapper.MapToMessage(justSaying);
        
        justSaying.RaisingComponent.ShouldBe(component);
        justSaying.Version.ShouldBe(version);

        message.Header.Id.ShouldBe(justSaying.Id);
        message.Header.CorrelationId.ShouldBe(Guid.Parse(justSaying.Conversation));
        message.Header.TimeStamp.ShouldBe(justSaying.TimeStamp);
        message.Header.ContentType.ShouldBe(MessageBody.APPLICATION_JSON);
        message.Header.Topic.ShouldBe($"{tenant}-{environment}-someevent");
        message.Header.MessageType.ShouldBe(MessageType.MT_EVENT);
        message.Header.Bag.ShouldContainKeyAndValue("Subject", typeof(SomeEvent).Name!);
        
        message.Body.ContentType.ShouldBe(MessageBody.APPLICATION_JSON);
        message.Body.Value.ShouldNotBeEmpty();
        
    }

    [Fact]
    public void MapToMessage_Using_Newtonsoft()
    {
        var version = Guid.NewGuid().ToString("N");
        var component = $"RaisingComponent{Guid.NewGuid():N}";
        var environment = $"Environment{Guid.NewGuid():N}";
        var topic = $"Topic{Guid.NewGuid():N}";

        var opts = Substitute.For<IOptionsSnapshot<JustSayingMapperConfiguration>>();
        opts.Get(Arg.Any<string>()).Returns(new JustSayingMapperConfiguration
        {
            Version = version,
            ComponentName = component,
            Environment = environment,
            Topic = topic,
            NewtonsoftSerializeOptions = new JsonSerializerSettings()
        });

        var mapper = new JustSayingMapper<SomeCommand>(opts);

        var justSaying = new SomeCommand
        {
            Message = "Hello World!",
        };

        var message = mapper.MapToMessage(justSaying);
        
        justSaying.Conversation.ShouldNotBeEmpty();
        justSaying.RaisingComponent.ShouldBe(component);
        justSaying.Version.ShouldBe(version);

        message.Header.Id.ShouldBe(justSaying.Id);
        message.Header.TimeStamp.ShouldBe(justSaying.TimeStamp);
        message.Header.ContentType.ShouldBe(MessageBody.APPLICATION_JSON);
        message.Header.CorrelationId.ShouldBe(Guid.Parse(justSaying.Conversation!));
        message.Header.Topic.ShouldBe(topic);
        message.Header.MessageType.ShouldBe(MessageType.MT_COMMAND);
        message.Header.Bag.ShouldContainKeyAndValue("Subject", typeof(SomeCommand).Name!);
        
        message.Body.ContentType.ShouldBe(MessageBody.APPLICATION_JSON);
        message.Body.Value.ShouldNotBeEmpty();
    }
}

public class SomeEvent : JustSayingEvent
{
    public string Message { get; set; } = string.Empty;
}

public class SomeCommand : JustSayingCommand
{
    public string Message { get; set; } = string.Empty;
}
