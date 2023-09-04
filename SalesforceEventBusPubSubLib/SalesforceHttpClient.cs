using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SalesforceEventBusPubSubLib
{
    public class SalesforceHttpClient
    {
        public string LoginEndpoint = "https://login.salesforce.com/services/oauth2/token";
        public string Username = ""; //change with your data
        public string Password = "";//change with your data
        public string Token = "";
        public string ClientId = "";//change with your data
        public string ClientSecret = "";//change with your data
        public string GrantType = "password";

        public readonly HttpClient client;
        public SalesforceHttpClient()
        {
            client = new HttpClient();
        }

        public SalesforceHttpClient(IConfiguration config) : this()
        {
            this.Username = config.GetSection("salesforce:Username").Value;
            this.GrantType = config.GetSection("salesforce:GrantType").Value;
            this.Password = config.GetSection("salesforce:Password").Value;
            this.ClientId = config.GetSection("salesforce:ClientId").Value;
            this.ClientSecret = config.GetSection("salesforce:ClientSecret").Value;
        }

        public SalesforceHttpClient(SalesforceConfig config) : this()
        {
            this.Username = config.Username;
            this.GrantType = config.GrantType;
            this.Password = config.Password;
            this.ClientId = config.ClientId;
            this.ClientSecret = config.ClientSecret;
        }

        public async Task<AuthResponse> GetToken()
        {
            HttpContent formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username",Username },
            { "password", Password },
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "grant_type", GrantType }
        });

            HttpRequestMessage requestMessage = new HttpRequestMessage();
            requestMessage.Content = formData;
            requestMessage.Method = HttpMethod.Post;
            requestMessage.RequestUri = new Uri(LoginEndpoint);

            HttpResponseMessage httpResponse = await client.SendAsync(requestMessage);
            var content = await httpResponse.Content.ReadAsStringAsync();
            AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(content)!;

            return authResponse;
        }
    }
}