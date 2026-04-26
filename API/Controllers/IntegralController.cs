using MathChain.API.DTOs;
using MathChain.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegralController: ControllerBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IMathProblemData _mathProblemRepository;

        public IntegralController(IBlockchainService blockchainService, IMathProblemData mathProblemRepository)
        {
            _blockchainService = blockchainService;
            _mathProblemRepository = mathProblemRepository;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSolutionAsync([FromBody] SubmitSolutionRequest request)
        {
            try
            {
                var transactionHash = await _blockchainService.SubmitSolutionAsync(
                    request.ProblemId, request.WalletAddress, request.Solution);
                return Ok(transactionHash);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifySolutionAsync([FromBody] VerifySolutionRequest request)
        {
            try
            {
                var result = await _blockchainService.VerifySolutionAsync(
                    request.ProblemId, request.Solution);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("pay")]
        public async Task<IActionResult> PayForSolutionAsync([FromBody] PayForSolutionRequest request)
        {
            try
            {
                var transactionHash = await _blockchainService.PayForSolutionAsync(
                    request.ProblemId, request.WalletAddress);
                return Ok(transactionHash);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("balance/{walletAddress}")]
        public async Task<IActionResult> GetWalletBalanceAsync(string walletAddress)
        {
            try
            {
                var balance = await _blockchainService.GetWalletBalanceAsync(walletAddress);
                return  Ok(balance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
