using Microsoft.AspNetCore.Mvc;
using Ultron.API.Services;

namespace Ultron.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController : ControllerBase
    {
        private readonly WhisperService _whisperService;

        public WhisperController(WhisperService whisperService)
        {
            _whisperService = whisperService;
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> Transcribe(IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest("No audio file provided.");

            using var stream = audio.OpenReadStream();
            var transcript = await _whisperService.TranscribeAsync(stream, audio.FileName);

            return Ok(new { transcript });
        }
    }
}