using Microsoft.AspNetCore.Mvc;
using TestJob.Models;
using TestJob.Services;

namespace TestJob.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevSecController : ControllerBase
    {
        private readonly IDevSecService _devSecService;

        public DevSecController(IDevSecService devSecService)
        {
            _devSecService = devSecService;
        }

        [HttpPost]
        public async Task<IActionResult> Post(RequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _devSecService.ProcessAsync(requestDto, cancellationToken);
            if (result.ErrorCode is "VALIDATION_ERROR" or "INVALID_URL_BASE64" or "INVALID_PAGE_BASE64" or "INVALID_SELECTOR"
                or "INVALID_KEY_BASE64" or "INVALID_CIPHERTEXT_BASE64"
                or "INVALID_KEY_LENGTH" or "INVALID_CIPHERTEXT_LENGTH")
                return BadRequest(result);

            return Ok(result);
        }
    }
}
