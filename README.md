# Brighter interop Justsaying
It's a project that allow you to integrate [Brighter](https://github.com/BrighterCommand/Brighter) with [Just Saying](https://github.com/justeattakeaway/JustSaying)
So you can send a message between these two libraries without any issue.


Today Brighter is able to read message from JustSaying but JustSaying isn't able to read 
Brighter message without a small change on Brighter mapper

# Usage

Add this package

```bash
dotnet package add Brighter.Interop.JustSaying
dotnet package add Brighter.Interop.JustSaying.Extensions.DependencyInjection
```

## Brighter 

All messages (command or event) should extend `JustSayingEvent`, `JustSayingCommmand` or `JustSayingMessage` or implement `IJustSayingMessage`

```c#
using Brighter.Interop.JustSaying;

public class SomeEvent : JustSayingEvent
{
}

public class SomeCommand : JustSayingCommand
{
}
```

During Brighter registration you need manually set the `JustSayingMapper` or use the extensions method  `AutoRegisterJustSayingMapper`

```c#
services
    .AddBrighter()
    .MapperRegistry(cfg =>
    {
        cfg.Register<SomeEvent, JustSayingMapper<SomeEvent>>();
        cfg.Register<SomeCommand, JustSayingMapper<SomeCommand>>();
    });
```

or 
```c#

services
    .AddBrighter()
    .AutoRegisterJustSayingMapper();
```

## JustSaying

For the integration no changes

But if you want to shared same queue/topic you need to :

```c#
config
    .Publications(build =>
    {
        build.WithTopic<SomeEvent>(cfg =>
        {
            cfg.WithTag("Source", "Brighter"); // Allow brighter to subscribe to that queue
        });
    })
```