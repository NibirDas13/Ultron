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
        private static List<object> _conversationHistory = new();

        public UltronController(ClaudeService claudeService, NewsService newsService)
        {
            _claudeService = claudeService;
            _newsService = newsService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest("Message cannot be empty.");

            var newsContext = "";

            var newsKeywords = new[] { "war", "conflict", "news", "world", "politics", 
                                       "economy", "crisis", "attack", "election", "israel", 
                                       "russia", "ukraine", "iran", "china", "us", "india" };

            var messageLower = request.Message.ToLower();
            var matchedKeyword = newsKeywords
                .FirstOrDefault(k => messageLower.Contains(k));

            if (matchedKeyword != null)
            {
                var headlines = await _newsService.GetTopHeadlinesAsync(matchedKeyword);
                newsContext = $"\n\nCurrent world news on '{matchedKeyword}':\n{headlines}\n\nUse this context to give a specific, informed response. Reference actual events.";
            }

            var fullMessage = request.Message + newsContext;

            _conversationHistory.Add(new
            {
                role = "user",
                content = fullMessage
            });

            var reply = await _claudeService.ChatAsync(_conversationHistory);

            _conversationHistory.Add(new
            {
                role = "assistant",
                content = reply
            });

            return Ok(new
            {
                reply,
                responseType = "text"
            });
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