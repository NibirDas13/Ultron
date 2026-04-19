using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ultron.API.Services
{
    public class VoiceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _voiceId;

        public VoiceService(IConfiguration configuration)
        {
            _apiKey = configuration["ElevenLabs:ApiKey"]
                ?? throw new Exception("ElevenLabs API key not found.");
            _voiceId = configuration["ElevenLabs:VoiceId"]
                ?? throw new Exception("ElevenLabs Voice ID not found.");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
        }

        public async Task<byte[]> TextToSpeechAsync(string text)
        {
            var url = $"https://api.elevenlabs.io/v1/text-to-speech/{_voiceId}";

            var requestBody = new
            {
                text,
                model_id = "eleven_turbo_v2_5",
                voice_settings = new
                {
                    stability = 0.75,
                    similarity_boost = 0.75,
                    style = 0.0,
                    use_speaker_boost = true
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"ElevenLabs error: {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}