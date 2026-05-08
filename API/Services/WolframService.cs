using System.Globalization;
using System.Text.RegularExpressions;
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
                string url = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(mathQuery)}&appid={_appId}";

                var response = await _client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string xmlResponse = await response.Content.ReadAsStringAsync();
                XDocument doc = XDocument.Parse(xmlResponse);

                var resultPod = doc.Descendants("pod").FirstOrDefault(p =>
                    (p.Attribute("primary") != null && p.Attribute("primary").Value.ToLower() == "true") ||
                    (p.Attribute("title") != null && (
                        p.Attribute("title").Value.ToLower() == "result" ||
                        p.Attribute("title").Value.ToLower() == "decimal approximation" ||
                        p.Attribute("title").Value.ToLower() == "definite integral"
                    )));

                if (resultPod == null)
                {
                    var titles = string.Join(", ", doc.Descendants("pod").Select(p => p.Attribute("title")?.Value));
                    return 0;
                }

                string resultText = resultPod.Descendants("plaintext").FirstOrDefault()?.Value;

                if (!string.IsNullOrEmpty(resultText))
                {
                    string rawText = resultText;

                    if (resultText.Contains("≈")) resultText = resultText.Split('≈').Last();
                    else if (resultText.Contains("=")) resultText = resultText.Split('=').Last();

                    resultText = resultText.Trim();

                    if (resultText.Contains("/"))
                    {
                        var parts = resultText.Split('/');
                        if (parts.Length == 2 &&
                            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator) &&
                            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator) &&
                            denominator != 0)
                        {
                            return numerator / denominator;
                        }
                    }

                    var match = Regex.Match(resultText, @"[-+]?[0-9]*\.?[0-9]+");
                    if (match.Success)
                    {
                        if (double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double exactResult))
                        {
                            return exactResult;
                        }
                    }

                    return 0;
                }

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
