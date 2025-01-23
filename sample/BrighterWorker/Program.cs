// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.Runtime;

using Brighter.Interop.JustSaying;
using Brighter.Interop.JustSaying.Mappers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.AWSSQS;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Serilog;

const string AppName = "JustSayingWorker";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .Enrich.WithProperty("AppName", AppName)
    .CreateLogger();

var host = new HostBuilder()
    .UseSerilog()
    .ConfigureServices((_, services) =>
    {
        var awsConnection = new AWSMessagingGatewayConnection(FallbackCredentialsFactory.GetCredentials(), RegionEndpoint.EUWest1);
        services
            .AddSingleton(new JustSayingMapperConfiguration
            {
                ComponentName = AppName,
                Environment = "qa28",
                Version = "1.0.0"
            })
            .AddHostedService<ServiceActivatorHostedService>()
            .AddServiceActivator(opt =>
            {
                opt.Subscriptions = new SqsSubscription[]
                {
                    new SqsSubscription<JustSayingMessageProducer>(
                        new SubscriptionName(nameof(JustSayingMessageProducer)),
                        new ChannelName("justsaying-to-brighter"), // The queue name
                        new RoutingKey(nameof(JustSayingMessageProducer).ToValidSNSTopicName()), // The topic Name
                        makeChannels: OnMissingChannel.Create)
                };
                opt.ChannelFactory = new ChannelFactory(awsConnection);
            })
            .UseExternalBus(new SnsProducerRegistryFactory(awsConnection,
                [
                    new()
                    {
                        Topic = new RoutingKey(nameof(BrighterMessageProducer).ToValidSNSTopicName()),
                        MakeChannels = OnMissingChannel.Create
                    }
                ]).Create())
            .Handlers(cfg => cfg.Register<JustSayingMessageProducer, JustSayingMessageHandler>())
            // .MapperRegistry(cfg =>
            // {
            //     cfg.Register<BrigtherMessageProducer, JustSayingMapper<BrigtherMessageProducer>>();
            //     cfg.Register<JustSayingMessageProducer, JustSayingMapper<JustSayingMessageProducer>>();
            // })
            .AutoRegisterJustSayingMapper()
            ;
    })
    .UseConsoleLifetime()
    .Build();

_ = host.RunAsync();

var processor = host.Services.GetRequiredService<IAmACommandProcessor>();
while (true)
{
    Console.WriteLine("Enter a message to send (q to quit):");
    var message = Console.ReadLine();
    if (message == "q")
    {
        break;
    }

    if (string.IsNullOrEmpty(message))
    {
        continue;
    }

    await processor.PostAsync(new BrighterMessageProducer
    {
        Text = message,
        Conversation = Guid.NewGuid().ToString(),
        RaisingComponent = AppName,
    });
}

await host.StopAsync();

public class JustSayingMessageProducer : JustSayingEvent 
{
    public string Text { get; set; } = string.Empty;
}

public class BrighterMessageProducer : JustSayingEvent 
{
    public string Text { get; set; } = string.Empty;
}

public class JustSayingMessageHandler(ILogger<JustSayingMessageHandler> logger) : RequestHandler<JustSayingMessageProducer>
{
    public override JustSayingMessageProducer Handle(JustSayingMessageProducer command)
    {
        logger.LogInformation("Receiving message form JustSaying {Text}", command.Text);
        return base.Handle(command);
    }
}