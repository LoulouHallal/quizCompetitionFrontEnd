using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerPointAddIn1.Helpers
{
    public static class NgrokHelper
    {
        public static async Task<string> GetNgrokBaseUrlAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://localhost:5000/api/ngrok-url");
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("🔍 ngrok response: " + json);
                    var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    return result["url"];
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}