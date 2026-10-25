using MathChain.Infrastructure.ResponseHandling;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MathChain.Infrastructure.Wolfram
{
    internal class WolframService : IWolframService
    {
        private readonly HttpClient _httpClient;
        private readonly string _appId;

        public WolframService(HttpClient httpClient, IConfiguration? configuration = null)
        {
            _httpClient = httpClient;
            _appId = configuration?["WOLFRAM_API_KEY"]
                ?? Environment.GetEnvironmentVariable("WOLFRAM_API_KEY")
                ?? "WOLFRAM_KEY_NOT_CONFIGURED";
        }

        public async Task<double> GetExactSolutionAsync(string mathQuery)
        {
            if (string.IsNullOrWhiteSpace(mathQuery))
            {
                return 0;
            }

            try
            {
                string url = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(mathQuery)}&appid={_appId}";

                var response = await _httpClient.GetAsync(url);

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

        public async Task<Envelope<double>> SolveWithEnvelopeAsync(string mathQuery)
        {
            if(string.IsNullOrWhiteSpace(mathQuery))
            {
                return EnvelopeExtensions.Failure<double>("The Mathematical Interogation cannot be empty.", 400);
            }

            try
            {
                double result = await GetExactSolutionAsync(mathQuery);

                return EnvelopeExtensions.Success(result, "The solution was successfully calculated.", 200);
            }
            catch(Exception ex)
            {
                return EnvelopeExtensions.Failure<double>($"Calculation error: {ex.Message}", 500);
            }
        }
    }
}
