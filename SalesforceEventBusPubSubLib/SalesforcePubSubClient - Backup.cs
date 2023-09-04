using Eventbus.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using SolTechnology.Avro;
using System.IO;

namespace SalesforceEventBusPubSubLib
{
    public class SalesforcePubSubClient
    {
        private readonly PubSub.PubSubClient _client;
        private readonly Metadata _metadata;
        private readonly ILogger _logger;

        public static Google.Protobuf.ByteString ReplyID { get; set; }

        public SalesforcePubSubClient(string address, Metadata metadata, ILogger logger)
        {
            var channelSalesforce = GrpcChannel.ForAddress(address);
            _client = new PubSub.PubSubClient(channelSalesforce);
            _metadata = metadata;
            _logger = logger;
        }

        public TopicInfo GetTopicByName(string platformEventName)
        {
            TopicRequest topicRequest = new TopicRequest
            {
                TopicName = platformEventName
            };

            return _client.GetTopic(request: topicRequest, headers: _metadata);
        }

        public SchemaInfo GetSchemaById(string schemaId)
        {
            SchemaRequest schemaRequest = new SchemaRequest
            {
                SchemaId = schemaId
            };

            return _client.GetSchema(request: schemaRequest, headers: _metadata);
        }

        public async Task Subscribe(string platformEventName, string jsonSchema)
        {
            await Subscribe(platformEventName, jsonSchema, new CancellationTokenSource());
        }
        public async Task Subscribe(string platformEventName, string jsonSchema, CancellationTokenSource source)
        {
            //var source = new CancellationTokenSource();

            try
            {

                using AsyncDuplexStreamingCall<FetchRequest, FetchResponse> stream = _client.Subscribe(headers: _metadata, cancellationToken: source.Token);

                FetchRequest fetchRequest = new FetchRequest
                {
                    TopicName = platformEventName,
                    ReplayPreset = ReplayPreset.Latest,
                    NumRequested = 2
                };

                if(ReplyID != null)
                {
                    fetchRequest.ReplayId = ReplyID;
                    fetchRequest.ReplayPreset = ReplayPreset.Custom;
                }

                await WriteToStream(stream.RequestStream, fetchRequest);

                await ReadFromStream(stream.ResponseStream, jsonSchema, source);

                //Task.Delay(1000).Wait();

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogError($"Operation Cancelled: {e.Message} Source {e.Source} {e.StackTrace}");
                //throw;

            }
        }

        public async Task WriteToStream(IClientStreamWriter<FetchRequest> requestStream, FetchRequest fetchRequest)
        {
            await requestStream.WriteAsync(fetchRequest);
        }

        public async Task ReadFromStream(IAsyncStreamReader<FetchResponse> responseStream, string jsonSchema, CancellationTokenSource source = null)
        {
            while (await responseStream.MoveNext())
            {
                _logger.LogInformation($"Time: {DateTime.Now} RPC ID: {responseStream.Current.RpcId}");

                if (responseStream.Current.Events != null)
                {
                    Console.WriteLine($"Number of events received: {responseStream.Current.Events.Count}");
                    foreach (var item in responseStream.Current.Events)
                    {

                        byte[] bytePayload = item.Event.Payload.ToByteArray();
                        string jsonPayload = AvroConvert.Avro2Json(bytePayload, jsonSchema);
                        _logger.LogInformation($"response: {jsonPayload}");

                        ReplyID = item.ReplayId;
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} Subscription is active");
                    _logger.LogInformation($"{DateTime.Now} Subscription is active");
                }
            }
        }
    }

}