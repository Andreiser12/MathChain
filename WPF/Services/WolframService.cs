using MathChain.Domain.Entities;
using Microsoft.VisualBasic.ApplicationServices;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace MathChain.WPF.Services
{
    class WolframService
    {
        private readonly string _appId = Environment.GetEnvironmentVariable("WOLFRAM_API_KEY");
        private readonly HttpClient _client;
        private readonly Random _random;

        public WolframService()
        {
            _client = new HttpClient();
            _random = new Random();
        }

        public (string wolframQuery, string latexDisplay) GenerateRandomPrimitive(Formula formula)
        {
            int a = _random.Next(2,10);
            int n = _random.Next(2, 6);

            if(formula.Latex.Contains("int a "))
            {
                string query = $"integrate {a}x^{n} from {lower} to {upper}";
                string latex = $"\\int_{lower}^{upper} {a}x^{n} \\,dx";

                return (query, latex);
            }

            
        }

        public async Task<double> GetExactSolutionAsync(string mathQuery)
        {
            try
            {
                string url = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(mathQuery)}&appid={_appId}";

                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();

                    return 0;
                }

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
                    System.Windows.MessageBox.Show($"Wolfram a ascuns rezultatul. Titlurile primite sunt: {titles}");
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
                            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numarator) &&
                            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double numitor) &&
                            numitor != 0)
                        {
                            return numarator / numitor;
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

                    MessageBox.Show($"Eșec la parsare. Textul brut de la Wolfram a fost: '{rawText}'");
                    return 0;
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare de sistem: \n" + ex.Message);
                return 0;
            }
        }
    }
}
