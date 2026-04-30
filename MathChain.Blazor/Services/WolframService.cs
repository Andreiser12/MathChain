using MathChain.Domain.Entities;
using Microsoft.Extensions.Logging;
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
                case "int a ":
                    query = $"integrate {a} from 0 to 1";
                    latex = $"\\int {a} \\,dx";
                    break;

                case "int x^n ":
                    query = $"integrate x^{n} from 0 to 1";
                    latex = $"\\int x^{n} \\,dx";
                    break;
            }


            return (query, latex);
        }
    }
}
