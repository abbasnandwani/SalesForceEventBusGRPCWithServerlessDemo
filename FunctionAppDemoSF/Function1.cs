using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SalesforceEventBusPubSubLib;
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using System.Dynamic;
using Newtonsoft.Json;

namespace FunctionAppDemoSF
{
    public class Function1
    {
        [FunctionName("SFScheduleTrigger")]
        public static async Task SFScheduleTrigger([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Blob("eventfiles", Connection = "blobconstr")] BlobContainerClient blobContainerClient,
            [ServiceBus("sfqueue", Microsoft.Azure.WebJobs.ServiceBus.ServiceBusEntityType.Queue, Connection = "sbconnstr")]
        ServiceBusSender  serviceBusSender,
            ILogger log)
        {
            log.LogInformation($"SFScheduleTrigger Triggered at {DateTime.Now}");


            #region Test
            //string filename = "test.txt";
            //var blobClient = blobContainerClient.GetBlobClient(filename);

            //await blobClient.UploadAsync(BinaryData.FromString("Hello world " + DateTime.Now), overwrite: true);

            //await outputSB.se.AddAsync(new ServiceBusMessage("Hello world " + DateTime.Now));

            //dynamic dynOutput = new ExpandoObject();

            //dynOutput.ChangeType = "update";
            //dynOutput.RecordId = "11";
            //dynOutput.EntityName = "Account";

            //var ser = JsonConvert.SerializeObject(dynOutput);

            //return;
            #endregion


            string Username = Environment.GetEnvironmentVariable("salesforce_Username");
            string GrantType = Environment.GetEnvironmentVariable("salesforce_GrantType");
            string Password = Environment.GetEnvironmentVariable("salesforce_Password");
            string ClientId = Environment.GetEnvironmentVariable("salesforce_ClientId");
            string ClientSecret = Environment.GetEnvironmentVariable("salesforce_ClientSecret");

            //log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}\n{Username}\n{GrantType}\n{Password}\n{ClientId}\n{ClientSecret}");
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            #region Get Salesforce Access Token 

            SalesforceConfig sconfig = new SalesforceConfig()
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                GrantType = GrantType,
                Password = Password,
                Username = Username
            };

            SalesforceHttpClient salesforceClient = new SalesforceHttpClient(sconfig);
            var auth = await salesforceClient.GetToken();
            string tenantId = Environment.GetEnvironmentVariable("salesforce_TenantId");
            #endregion

            #region Call Salesforce Pub/Sub Client
            Metadata metadata = new Metadata{
    { "accesstoken", auth.AccessToken},
    { "instanceurl", auth.InstanceUrl},
    { "tenantid", tenantId}
};
            string pubSubEndpoint = Environment.GetEnvironmentVariable("salesforce_pub_sub_endpoint");
            string platformEventName = Environment.GetEnvironmentVariable("salesforce_platform_event_name");
            SalesforcePubSubClient salesforcePubSubClient = new SalesforcePubSubClient(pubSubEndpoint, metadata, log);

            //set blob contaier reference
            salesforcePubSubClient.BlobContainerClient = blobContainerClient;
            salesforcePubSubClient.ServiceBusSender = serviceBusSender;

            var topic = salesforcePubSubClient.GetTopicByName(platformEventName);
            var schema = salesforcePubSubClient.GetSchemaById(topic.SchemaId);

            while (true)
            {
                log.LogInformation($"Listening to Event Bus {platformEventName}....");

                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromSeconds(30));
                //await salesforcePubSubClient.Subscribe(platformEventName, schema.SchemaJson);
                await salesforcePubSubClient.Subscribe(platformEventName, schema.SchemaJson, source);

                log.LogInformation("Listening to Event Bus Complete");

                log.LogInformation("Waiting for 5 seconds");
                Task.Delay(5000).Wait();
                break;
            }

            //log.LogInformation("Listening to Event Bus....");
            //CancellationTokenSource source = new CancellationTokenSource();
            //source.CancelAfter(TimeSpan.FromSeconds(30));
            ////await salesforcePubSubClient.Subscribe(platformEventName, schema.SchemaJson);
            //await salesforcePubSubClient.Subscribe(platformEventName, schema.SchemaJson, source);

            //log.LogInformation("Listening to Event Bus Complete");
            #endregion

        }
    }
}
