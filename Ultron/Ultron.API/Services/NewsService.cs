using System.Text.Json;

namespace Ultron.API.Services
{
    public class NewsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NewsService(IConfiguration configuration)
        {
            _apiKey = configuration["NewsApi:ApiKey"]
                ?? throw new Exception("NewsAPI key not found.");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "UltronApp/1.0");
        }

        public async Task<string> GetTopHeadlinesAsync(string query = "world")
        {
            var url = $"https://newsapi.org/v2/everything?q={query}&sortBy=publishedAt&pageSize=5&apiKey={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"NewsAPI error: {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var articles = doc.RootElement.GetProperty("articles");

            var headlines = new List<string>();

            foreach (var article in articles.EnumerateArray())
            {
                var title = article.GetProperty("title").GetString();
                var description = article.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : "";

                if (!string.IsNullOrEmpty(title))
                    headlines.Add($"- {title}: {description}");
            }

            return string.Join("\n", headlines);
        }
    }
}