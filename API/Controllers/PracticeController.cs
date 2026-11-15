using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    [ApiController]
    [Route("api/practice")]
    public class PracticeController : ApiControllerBase
    {
        private readonly IPracticeContext _practiceContext;

        public PracticeController(IPracticeContext practiceContext)
        {
            _practiceContext = practiceContext;
        }

        [HttpGet("formulas/{formulaId:guid}")]
        public async Task<IActionResult> GetFormula(Guid formulaId)
        {
            var formula = await _practiceContext.GetFormulaAsync(formulaId);

            return Success(formula);
        }

        [HttpPost("formulas/{formulaId:guid}/exercises/{index:int}")]
        public async Task<IActionResult> StartExercise(Guid formulaId, int index)
        {
            var exercise = await _practiceContext.StartExerciseAsync(formulaId, index);

            return Success(exercise);
        }

        [HttpPost("exercises/{exerciseId:guid}/answer")]
        public async Task<IActionResult> SubmitAnswer(Guid exerciseId, [FromBody] SubmitPracticeAnswerRequest request)
        {
            var result = await _practiceContext.SubmitAnswerAsync(exerciseId, request);

            return Success(result, result.Message);
        }
    }
}
