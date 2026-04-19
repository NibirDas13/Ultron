using Microsoft.AspNetCore.Mvc;
using Ultron.API.Services;

namespace Ultron.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : ControllerBase
    {
        private readonly VoiceService _voiceService;

        public VoiceController(VoiceService voiceService)
        {
            _voiceService = voiceService;
        }

        [HttpPost("speak")]
        public async Task<IActionResult> Speak([FromBody] SpeakRequest request)
        {
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest("Text cannot be empty.");

            var audioBytes = await _voiceService.TextToSpeechAsync(request.Text);
            return File(audioBytes, "audio/mpeg");
        }
    }

    public class SpeakRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}