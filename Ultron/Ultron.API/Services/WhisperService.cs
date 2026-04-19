using System.Net.Http.Headers;

namespace Ultron.API.Services
{
    public class WhisperService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WhisperService(IConfiguration configuration)
        {
            _apiKey = configuration["Groq:ApiKey"]
                ?? throw new Exception("Groq API key not found.");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> TranscribeAsync(Stream audioStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            
            var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
            content.Add(audioContent, "file", "recording.webm");
            content.Add(new StringContent("whisper-large-v3"), "model");

            var response = await _httpClient.PostAsync(
                "https://api.groq.com/openai/v1/audio/transcriptions", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Whisper error: {responseBody}");

            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("text").GetString() ?? "";
        }
    }
}