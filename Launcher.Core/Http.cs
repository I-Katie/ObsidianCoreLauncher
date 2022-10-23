using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Launcher.Core
{
    class Http
    {
        internal static async Task<string> GetStringAsync(string url)
        {
            HttpClient hc = new HttpClient();

            var response = await hc.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpFailureResponseException(response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync();
        }

        internal static async Task<(string, DateTime?)> GetStringWithDateAsync(string url)
        {
            HttpClient hc = new HttpClient();

            var response = await hc.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpFailureResponseException(response.StatusCode);
            }

            return (await response.Content.ReadAsStringAsync(), response.Headers.Date?.LocalDateTime.ToUniversalTime());
        }

        internal static async Task GetFileAsync(string url, string outputFile)
        {
            HttpClient hc = new HttpClient();

            var response = await hc.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpFailureResponseException(response.StatusCode);
            }

            using (var fs = new FileStream(outputFile, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        internal static async Task<HttpResponseMessage> HeadAsync(string url, Dictionary<string, string> headers = null)
        {
            HttpClient hc = new HttpClient();

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    hc.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                }
            }

            return await hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        }
    }

    public class HttpFailureResponseException : IOException
    {
        public HttpFailureResponseException(HttpStatusCode statusCode) : base($"Server returned code '{statusCode}'")
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
