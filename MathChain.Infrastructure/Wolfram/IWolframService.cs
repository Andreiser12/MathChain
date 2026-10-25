using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Infrastructure.Wolfram
{
    public interface IWolframService
    {
        Task<double> GetExactSolutionAsync(string mathQuery);
        Task<Envelope<double>> SolveWithEnvelopeAsync(string mathQuery);
    }
}
