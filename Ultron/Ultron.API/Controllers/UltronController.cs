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
        private readonly CosmosDbService _cosmosDbService;
        private const string UserId = "avi";

        public UltronController(ClaudeService claudeService, NewsService newsService, 
            SpotifyService spotifyService, CosmosDbService cosmosDbService)
        {
            _claudeService = claudeService;
            _newsService = newsService;
            _spotifyService = spotifyService;
            _cosmosDbService = cosmosDbService;
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
                    var spotifyReply = string.IsNullOrEmpty(query)
                        ? "Resuming. Try to appreciate it this time."
                        : $"Playing {query}. You could have worse taste.";
                    return Ok(new { reply = spotifyReply, responseType = "text" });
                }
            }

            // News intent detection
            var newsKeywords = new[] { "war", "conflict", "latest news", "what's happening",
                                       "world news", "current events", "headlines" };
            var matchedKeyword = newsKeywords.FirstOrDefault(k => messageLower.Contains(k));
            var newsContext = "";

            if (matchedKeyword != null)
            {
                var headlines = await _newsService.GetTopHeadlinesAsync(matchedKeyword);
                newsContext = $"\n\nCurrent world news on '{matchedKeyword}':\n{headlines}\n\nUse this context to give a specific, informed response. Reference actual events.";
            }

            // Load conversation history from Cosmos DB
            var history = await _cosmosDbService.GetConversationHistoryAsync(UserId);
            var fullMessage = request.Message + newsContext;

            // Save user message
            await _cosmosDbService.SaveMessageAsync(UserId, "user", fullMessage);

            // Get AI response
            var reply = await _claudeService.ChatAsync(history.Concat(
                new[] { new { role = "user", content = fullMessage } as object }).ToList());

            // Save assistant response
            await _cosmosDbService.SaveMessageAsync(UserId, "assistant", reply);

            return Ok(new { reply, responseType = "text" });
        }

        [HttpDelete("reset")]
        public async Task<IActionResult> Reset()
        {
            await _cosmosDbService.ClearHistoryAsync(UserId);
            return Ok(new { message = "Memory wiped. Starting fresh." });
        }

        [HttpGet("conflict-zones")]
        public async Task<IActionResult> GetConflictZones()
        {
            var keywords = new[] { "war", "conflict", "attack", "strike", "battle", "crisis" };
            var allHeadlines = new List<string>();

            foreach (var keyword in keywords)
            {
                var headlines = await _newsService.GetTopHeadlinesAsync(keyword);
                allHeadlines.Add(headlines);
            }

            var combinedNews = string.Join("\n", allHeadlines);

            var prompt = $@"Based on these current news headlines, identify active conflict zones. Return ONLY a valid JSON array. No explanation, no markdown, no extra text.
                            Each entry must be complete and properly closed.Format exactly like this:
                            [
                            {{""name"": ""Country/Region"", ""lat"": 0.0, ""lng"": 0.0, ""description"": ""Brief one line description""}},
                            {{""name"": ""Country/Region"", ""lat"": 0.0, ""lng"": 0.0, ""description"": ""Brief one line description""}}
                            ]

                            Headlines:
                            {combinedNews}

                            Return maximum 8 high conflict zones. Keep descriptions under 10 words. Only return the JSON array. Ensure the JSON is complete and valid.";

            var history = new List<object>
            {
                new { role = "user", content = prompt }
            };

            var reply = await _claudeService.ChatAsync(history);

            try
            {
                var cleanJson = reply.Trim();
                if (cleanJson.Contains("["))
                    cleanJson = cleanJson.Substring(cleanJson.IndexOf("["));
                if (cleanJson.Contains("]"))
                    cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("]") + 1);

                return Ok(cleanJson);
            }
            catch
            {
                return Ok("[]");
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}