using IDWallet.Interfaces;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Agent
{
    public class CustomHttpMessageDispatcher : IMessageDispatcher
    {
        protected readonly HttpClient HttpClient;

        public CustomHttpMessageDispatcher()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.Proxy = DependencyService.Get<IProxyInfoProvider>().GetProxySettings();
            HttpClient = new HttpClient(httpClientHandler);
        }

        public string[] TransportSchemes => new[] { "http", "https" };

        public async Task<PackedMessageContext> DispatchAsync(Uri endpointUri, PackedMessageContext message)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = endpointUri,
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(message.Payload)
            };

            MediaTypeHeaderValue agentContentType =
                new MediaTypeHeaderValue(DefaultMessageService.AgentWireMessageMimeType);
            request.Content.Headers.ContentType = agentContentType;

            HttpResponseMessage response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                throw new AriesFrameworkException(
                    ErrorCode.A2AMessageTransmissionError,
                    $"Failed to send A2A message with an HTTP status code of {response.StatusCode} and content {responseBody}");
            }

            if (response.Content?.Headers.ContentType?.Equals(agentContentType) ?? false)
            {
                byte[] rawContent = await response.Content.ReadAsByteArrayAsync();

                if (rawContent.Length > 0)
                {
                    return new PackedMessageContext(rawContent);
                }
            }

            return null;
        }
    }
}