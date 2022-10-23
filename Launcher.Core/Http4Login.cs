using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Launcher.Core
{
    static class Http4Login
    {
        internal static async Task<T> PostObjectAsync<T>(string url, object data, Dictionary<string, string> headers = null)
        {
            StringContent content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            HttpClient hc = new HttpClient();

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    hc.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                }
            }

            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await hc.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpFailureResponseException(response.StatusCode);
            }

            return await ReadContentAsync<T>(response);
        }

        internal static async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            HttpClient hc = new HttpClient();

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    hc.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                }
            }

            var response = await hc.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpFailureResponseException(response.StatusCode);
            }

            return await ReadContentAsync<T>(response);
        }

        private static async Task<T> ReadContentAsync<T>(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();

            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(text, typeof(T));
            else
                return JsonSerializer.Deserialize<T>(text);
        }
    }
}
