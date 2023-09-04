using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SalesforceEventBusPubSub
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            #region Add Console Logger
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging((loggingBuilder) => loggingBuilder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole()
                    )
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            logger.LogInformation("Testing Salesforce Pub/Sub API with .NET");

            #endregion

            #region Add Json Settings
            var builder = new ConfigurationBuilder()
                 .AddJsonFile($"appsettings.json");
            var config = builder.Build();
            #endregion

            #region Get Salesforce Access Token 

            SalesforceHttpClient salesforceClient = new SalesforceHttpClient(config);
            var auth = await salesforceClient.GetToken();
            string tenantId = config.GetSection("salesforce:tenant_id").Value!;
            #endregion

            #region Call Salesforce Pub/Sub Client
            Metadata metadata = new Metadata{
    { "accesstoken", auth.AccessToken},
    { "instanceurl", auth.InstanceUrl},
    { "tenantid", tenantId}
};
            string pubSubEndpoint = config.GetSection("salesforce:pub_sub_endpoint").Value!;
            string platformEventName = config.GetSection("salesforce:platform_event_name").Value!;
            SalesforcePubSubClient salesforcePubSubClient = new SalesforcePubSubClient(pubSubEndpoint, metadata, logger);

            var topic = salesforcePubSubClient.GetTopicByName(platformEventName);
            var schema = salesforcePubSubClient.GetSchemaById(topic.SchemaId);

            while (true)
            {
                await salesforcePubSubClient.Subscribe(platformEventName, schema.SchemaJson);
            }
            #endregion
        }
    }

}