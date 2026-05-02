using MathChain.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static MudBlazor.CategoryTypes;

namespace MathChain.Blazor.Services
{
    public class WolframService
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
            int a = _random.Next(2, 10);
            int n = _random.Next(2, 6);
            string query = "";
            string latex = "";

            string function = formula.Latex;

            switch(function)
            {
                case @"$$\int a \,dx = ax + C$$":
                    query = $"integrate {a} from 0 to 1";
                    latex = $"\\int {a} \\,dx";
                    break;

                case @"$$\int x^n \,dx = \frac{x^{n+1}}{n+1} + C$$":
                    query = $"integrate {a}x^{n} from 0 to 1";
                    latex = $"\\int {a}x^{n} \\,dx";
                    break;
                default:
                    break;
            }

            return (query, latex);
        }

        public async Task<double> GetExactSolutionAsync(string query)
        {
            try
            {
                var response = await _client.GetAsync(
                    $"http://localhost:5065/api/wolfram/solve?query={Uri.EscapeDataString(query)}");

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                return double.Parse(content, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Console.WriteLine("API call error: " + ex.Message);
                return 0;
            }
        }



    }
}
