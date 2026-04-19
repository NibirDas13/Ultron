using Microsoft.AspNetCore.Mvc;
using Ultron.API.Services;

namespace Ultron.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyService _spotifyService;

        public SpotifyController(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var url = _spotifyService.GetAuthUrl();
            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            await _spotifyService.ExchangeCodeAsync(code);
            return Ok(new { message = "Spotify connected. Ultron now controls your music." });
        }

        [HttpPost("play")]
        public async Task<IActionResult> Play([FromBody] PlayRequest request)
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected. Visit /api/spotify/login first." });

            await _spotifyService.PlayAsync(request.Query);
            return Ok(new { message = "Playing." });
        }

        [HttpPost("pause")]
        public async Task<IActionResult> Pause()
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected." });

            await _spotifyService.PauseAsync();
            return Ok(new { message = "Paused." });
        }

        [HttpPost("next")]
        public async Task<IActionResult> Next()
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected." });

            await _spotifyService.NextAsync();
            return Ok(new { message = "Skipped." });
        }

        [HttpPost("previous")]
        public async Task<IActionResult> Previous()
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected." });

            await _spotifyService.PreviousAsync();
            return Ok(new { message = "Previous track." });
        }

        [HttpPost("volume")]
        public async Task<IActionResult> Volume([FromBody] VolumeRequest request)
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected." });

            await _spotifyService.SetVolumeAsync(request.Level);
            return Ok(new { message = $"Volume set to {request.Level}." });
        }

        [HttpGet("current")]
        public async Task<IActionResult> Current()
        {
            if (!_spotifyService.IsAuthenticated)
                return Unauthorized(new { message = "Spotify not connected." });

            var track = await _spotifyService.GetCurrentTrackAsync();
            return Ok(new { track });
        }
    }

    public class PlayRequest
    {
        public string? Query { get; set; }
    }

    public class VolumeRequest
    {
        public int Level { get; set; }
    }
}