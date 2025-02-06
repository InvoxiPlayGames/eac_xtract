using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eac_xtract
{
    internal class EACModulesCDN
    {
        public static string MODULES_FORMAT = "https://modules-cdn.eac-prod.on.epicgames.com/modules/{0}/{1}/{2}";

        public async static Task<int> DoesModuleExist(string productId, string deploymentId, string platform)
        {
            string download_url = string.Format(MODULES_FORMAT, productId, deploymentId, platform);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("User-Agent", "EasyAntiCheat-Client/1.0");
            HttpRequestMessage req = new(HttpMethod.Head, download_url);
            HttpResponseMessage? resp = await client.SendAsync(req);

            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                return 0;

            return (int)resp.Content.Headers.ContentLength;
        }

        public async static Task<byte[]?> DownloadModule(string productId, string deploymentId, string platform)
        {
            string download_url = string.Format(MODULES_FORMAT, productId, deploymentId, platform);
            
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("User-Agent", "EasyAntiCheat-Client/1.0");
            HttpResponseMessage? resp = await client.GetAsync(download_url);

            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            byte[] response_data = await resp.Content.ReadAsByteArrayAsync();
            return response_data;
        }
    }
}
