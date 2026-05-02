using System.Globalization;
using System.Xml.Linq;

namespace MathChain.API.Services
{
    public class WolframService
    {
        private readonly string _appId;
        private readonly HttpClient _client;

        public WolframService(HttpClient client)
        {
            _client = client;
            _appId = Environment.GetEnvironmentVariable("WOLFRAM_API_KEY") ??
                throw new ArgumentNullException("WOLFRAM_API_KEY not found in configuration");
        }

        public async Task<double> GetExactSolutionAsync(string mathQuery)
        {
            try
            {
                string url = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(mathQuery)}&appid={_appId}&format=plaintext";

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string xmlResponse = await response.Content.ReadAsStringAsync();
                XDocument doc = XDocument.Parse(xmlResponse);

                var resultPod = doc.Descendants("pod")
                    .FirstOrDefault(p => p.Attribute("title")?.Value == "Definite integral"
                                      || p.Attribute("title")?.Value == "Result");

                if (resultPod == null)
                    return 0;

                var plaintext = resultPod.Descendants("plaintext").FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(plaintext))
                    return 0;

                if (double.TryParse(plaintext, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    return result;

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wolfram error: " + ex.Message);
                return 0;
            }
        }
    }
}
