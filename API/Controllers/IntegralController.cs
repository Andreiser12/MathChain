using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using MathChain.Domain.Interfaces;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegralController: ControllerBase
    {
        private readonly IBlockchainService _blockchainService;

        public IntegralController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSolutionAsync([FromBody] SubmitSolutionRequest request)
        {
            var transactionHash = await _blockchainService.SubmitSolutionAsync(
                request.ProblemId, request.WalletAddress, request.Solution);
            return Ok(transactionHash);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifySolutionAsync([FromBody] VerifySolutionRequest request)
        {
            var result = await _blockchainService.VerifySolutionAsync(
                request.ProblemId, request.Solution);
            return Ok(result);
        }

        [HttpPost("pay")]
        public async Task<IActionResult> PayForSolutionAsync([FromBody] PayForSolutionRequest request)
        {
            var transactionHash = await _blockchainService.PayForSolutionAsync(
                request.ProblemId, request.WalletAddress);
            return Ok(transactionHash);
        }

        [HttpGet("balance/{walletAddress}")]
        public async Task<IActionResult> GetWalletBalanceAsync(string walletAddress)
        {
            var balance = await _blockchainService.GetWalletBalanceAsync(walletAddress);
            return Ok(balance);
        }
    }
}
