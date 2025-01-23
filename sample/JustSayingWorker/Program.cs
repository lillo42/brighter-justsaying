﻿// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using JustSaying;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

const string AppName = "JustSayingWorker";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .Enrich.WithProperty("AppName", AppName)
    .CreateLogger();

var host = new HostBuilder()
    .UseSerilog()
    .ConfigureServices((_, services) =>
    {
        services.AddJustSaying(config =>
        {
            config
                .Client(build => build.WithCredentials(FallbackCredentialsFactory.GetCredentials()))
                .Messaging(cfg => cfg.WithRegion(RegionEndpoint.EUWest1))
                .Publications(build =>
                {
                    build.WithTopic<JustSayingMessageProducer>(cfg =>
                    {
                        cfg.WithTopicName(nameof(JustSayingMessageProducer))
                            .WithTag("Source", "Brighter"); // Allow brighter to subscribe to that queue
                    });
                })
                .Subscriptions(builder =>
                {
                    builder
                        .ForTopic<BrigtherMessageProducer>(cfg =>
                        {
                            cfg
                                .WithTopicName(nameof(BrigtherMessageProducer))
                                .WithQueueName("brighter-to-justsaying");
                        });
                });
        });

        services.AddJustSayingHandler<BrigtherMessageProducer, BrigtherMessageProducerHandler>();
    })
    .UseConsoleLifetime()
    .Build();

_ = host.RunAsync();

var bus = host.Services.GetRequiredService<IMessagingBus>();
await bus.StartAsync(CancellationToken.None);

var publisher = host.Services.GetRequiredService<IMessagePublisher>();
await publisher.StartAsync(CancellationToken.None);

while (true)
{
    Console.WriteLine("Enter a message to be send (q to quit):");
    var message = Console.ReadLine();
    if (message == "q")
    {
        break;
    }

    if (string.IsNullOrEmpty(message))
    {
        continue;
    }

    await publisher.PublishAsync(new JustSayingMessageProducer
    {
        Text = message,
        Conversation = Guid.NewGuid().ToString(),
        RaisingComponent = AppName,
    });
}

await host.StopAsync();


public class JustSayingMessageProducer : Message
{
    public string Text { get; set; } = string.Empty;
}

public class BrigtherMessageProducer : Message
{
    public string Text { get; set; } = string.Empty;
}

public class BrigtherMessageProducerHandler : IHandlerAsync<BrigtherMessageProducer>
{
    private readonly ILogger<BrigtherMessageProducerHandler> _logger;

    public BrigtherMessageProducerHandler(ILogger<BrigtherMessageProducerHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> Handle(BrigtherMessageProducer message)
    {
        _logger.LogInformation("Received message from brigther: {Message}", message.Text);
        return Task.FromResult(true);
    }
}