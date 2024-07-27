using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace PZServerDiscordBot.PZServer
{

    class PostQuery
    {
        public string eventName { get; set; }
        public string eventContent { get; set; }

    }

    internal class ServerAPI
    {

        private static string _gateway;
        private static string _path;
        private static int _port;

        public ServerAPI(string gateway = "localhost", int port = 13250, string path = "Console")
        {
            _gateway = gateway;
            _port = port;
            _path = path;
        }

        public async void SendQuery(string eventName, string eventContent)
        {
            PostQuery query = new PostQuery();
            query.eventName = eventName;
            query.eventContent = eventContent;
            await PostRequestAsync(JsonConvert.SerializeObject(query));
        }

        public async void ProcessOutput(DataReceivedEventArgs outputLine)
        {
            PostQuery query = new PostQuery();
            query.eventName = "Console";
            query.eventContent = outputLine.Data;
            await PostRequestAsync(JsonConvert.SerializeObject(query));
        }

        private async Task<string> PostRequestAsync(string jsonContent)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync($"http://{_gateway}:{_port}/{_path}", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

    }

}