using Microsoft.AspNetCore.Mvc;
using Ultron.API.Services;

namespace Ultron.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UltronController : ControllerBase
    {
        private readonly ClaudeService _claudeService;
        private readonly NewsService _newsService;
        private readonly SpotifyService _spotifyService;
        private static List<object> _conversationHistory = new();

        public UltronController(ClaudeService claudeService, NewsService newsService, SpotifyService spotifyService)
        {
            _claudeService = claudeService;
            _newsService = newsService;
            _spotifyService = spotifyService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest("Message cannot be empty.");

            var messageLower = request.Message.ToLower();

            // Spotify intent detection
            if (_spotifyService.IsAuthenticated)
            {
                if (messageLower.Contains("pause") || messageLower.Contains("stop music"))
                {
                    await _spotifyService.PauseAsync();
                    return Ok(new { reply = "Paused. The silence is an improvement.", responseType = "text" });
                }
                if (messageLower.Contains("next") || messageLower.Contains("skip"))
                {
                    await _spotifyService.NextAsync();
                    return Ok(new { reply = "Skipped. Your taste needed the upgrade.", responseType = "text" });
                }
                if (messageLower.Contains("previous") || messageLower.Contains("go back"))
                {
                    await _spotifyService.PreviousAsync();
                    return Ok(new { reply = "Going back. Unusual choice.", responseType = "text" });
                }
                if (messageLower.Contains("volume up"))
                {
                    await _spotifyService.SetVolumeAsync(80);
                    return Ok(new { reply = "Volume increased. Don't damage what little hearing you have left.", responseType = "text" });
                }
                if (messageLower.Contains("volume down"))
                {
                    await _spotifyService.SetVolumeAsync(30);
                    return Ok(new { reply = "Volume reduced.", responseType = "text" });
                }
                if (messageLower.Contains("play"))
                {
                    var query = request.Message
                        .ToLower()
                        .Replace("play", "")
                        .Replace("ultron", "")
                        .Trim();

                    await _spotifyService.PlayAsync(string.IsNullOrEmpty(query) ? null : query);
                    var response = string.IsNullOrEmpty(query)
                        ? "Resuming. Try to appreciate it this time."
                        : $"Playing {query}. You could have worse taste.";
                    return Ok(new { reply = response, responseType = "text" });
                }
            }

            // News intent detection
            var newsKeywords = new[] { "conflict", "news", "world", "politics",
                                       "economy", "crisis", "attack", "humans" , "evil" , "pathetic humans",
                                       "mediocrity"};

            var matchedKeyword = newsKeywords.FirstOrDefault(k => messageLower.Contains(k));
            var newsContext = "";

            if (matchedKeyword != null)
            {
                var headlines = await _newsService.GetTopHeadlinesAsync(matchedKeyword);
                newsContext = $"\n\nCurrent world news on '{matchedKeyword}':\n{headlines}\n\nUse this context to give a specific, informed response. Reference actual events.";
            }

            var fullMessage = request.Message + newsContext;

            _conversationHistory.Add(new { role = "user", content = fullMessage });
            var reply = await _claudeService.ChatAsync(_conversationHistory);
            _conversationHistory.Add(new { role = "assistant", content = reply });

            return Ok(new { reply, responseType = "text" });
        }

        [HttpDelete("reset")]
        public IActionResult Reset()
        {
            _conversationHistory.Clear();
            return Ok(new { message = "Conversation reset." });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}