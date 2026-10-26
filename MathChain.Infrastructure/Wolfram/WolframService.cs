using AngouriMath;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.Extensions.Configuration;
using org.mariuszgromada.math.mxparser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MathChain.Infrastructure.Wolfram
{
    public class WolframService : IWolframService
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

            string normalized = mathQuery.Trim();

            if (TrySolveWithMxParser(normalized, out double mxResult))
            {
                return mxResult;
            }

            if (TrySolveWithAngouri(normalized, out double angouriResult))
            {
                return angouriResult;
            }

            return await CallWolframAlphaAsync(normalized);
        }

        public async Task<Envelope<double>> SolveWithEnvelopeAsync(string mathQuery)
        {
            if (string.IsNullOrWhiteSpace(mathQuery))
            {
                return EnvelopeExtensions.Failure<double>("The Mathematical Interrogation cannot be empty.", 400);
            }

            try
            {
                double result = await GetExactSolutionAsync(mathQuery);
                return EnvelopeExtensions.Success(result, "The solution was successfully calculated.", 200);
            }
            catch (Exception ex)
            {
                return EnvelopeExtensions.Failure<double>($"Calculation error: {ex.Message}", 500);
            }
        }

        private bool TrySolveWithMxParser(string query, out double result)
        {
            result = 0;

            try
            {
                string exprStr = query;

                var match = Regex.Match(query, @"integrate\s+(.+?)\s+from\s+([-\d\.]+)\s+to\s+([-\d\.]+)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string func = match.Groups[1].Value.Trim();
                    string from = match.Groups[2].Value.Trim();
                    string to = match.Groups[3].Value.Trim();

                    func = Regex.Replace(func, @"(\d)([a-zA-Z])", "$1*$2");
                    exprStr = $"int({func}, x, {from}, {to})";
                }
                else
                {
                    exprStr = Regex.Replace(exprStr, @"(\d)([a-zA-Z])", "$1*$2");
                }

                var expression = new Expression(exprStr);
                double val = expression.calculate();

                if (!double.IsNaN(val) && !double.IsInfinity(val))
                {
                    result = Math.Round(val, 6);
                    return true;
                }
            }
            catch { }

            return false;
        }

        private bool TrySolveWithAngouri(string query, out double result)
        {
            result = 0;

            try
            {
                string exprStr = query;

                if (exprStr.Contains("=") && !exprStr.StartsWith("int", StringComparison.OrdinalIgnoreCase))
                {
                    Entity eq = exprStr;
                    var solutions = eq.Solve("x");
                    Entity? firstReal = null;

                    if (solutions is Entity.Set.FiniteSet finiteSet)
                    {
                        firstReal = finiteSet.Elements.FirstOrDefault();
                    }
                    else if (solutions is IEnumerable<Entity> enumSolutions)
                    {
                        firstReal = enumSolutions.FirstOrDefault();
                    }

                    if (firstReal != null)
                    {
                        var evaluated = firstReal.EvalNumerical();
                        result = Math.Round((double)evaluated, 6);
                        return true;
                    }
                }

                var match = Regex.Match(query, @"integrate\s+(.+?)\s+from\s+([-\d\.]+)\s+to\s+([-\d\.]+)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string func = match.Groups[1].Value.Trim();
                    string from = match.Groups[2].Value.Trim();
                    string to = match.Groups[3].Value.Trim();

                    func = Regex.Replace(func, @"(\d)([a-zA-Z])", "$1*$2");
                    Entity intExpr = $"integrate({func}, x, {from}, {to})";

                    var eval = intExpr.EvalNumerical();
                    result = Math.Round((double)eval, 6);
                    return true;
                }

                Entity general = exprStr;
                var num = general.EvalNumerical();
                result = Math.Round((double)num, 6);
                return true;
            }
            catch { }

            return false;
        }

        private async Task<double> CallWolframAlphaAsync(string mathQuery)
        {
            if (string.IsNullOrEmpty(_appId) || _appId == "WOLFRAM_KEY_NOT_CONFIGURED")
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
                    return 0;
                }

                string? resultText = resultPod.Descendants("plaintext").FirstOrDefault()?.Value;

                if (!string.IsNullOrEmpty(resultText))
                {
                    if (resultText.Contains("≈"))
                    {
                        resultText = resultText.Split('≈').Last();
                    }
                    else if (resultText.Contains("="))
                    {
                        resultText = resultText.Split('=').Last();
                    }

                    resultText = resultText.Trim();

                    if (resultText.Contains("/"))
                    {
                        var parts = resultText.Split('/');

                        if (parts.Length == 2 &&
                            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator) &&
                            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator) &&
                            denominator != 0)
                        {
                            return Math.Round(numerator / denominator, 6);
                        }
                    }

                    var numberMatch = Regex.Match(resultText, @"[-+]?[0-9]*\.?[0-9]+");

                    if (numberMatch.Success && double.TryParse(numberMatch.Value, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double exactResult))
                    {
                        return Math.Round(exactResult, 6);
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
