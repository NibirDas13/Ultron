using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ultron.API.Services
{
    public class SpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private string _accessToken = "";
        private string _refreshToken = "";
        private readonly string _tokenPath = @"D:\Project Ultron\spotify_tokens.json";

        public SpotifyService(IConfiguration configuration)
        {
            _clientId = configuration["Spotify:ClientId"]
                ?? throw new Exception("Spotify ClientId not found.");
            _clientSecret = configuration["Spotify:ClientSecret"]
                ?? throw new Exception("Spotify ClientSecret not found.");
            _redirectUri = configuration["Spotify:RedirectUri"]
                ?? throw new Exception("Spotify RedirectUri not found.");

            _httpClient = new HttpClient();
            LoadTokens();
        }

        private void LoadTokens()
        {
            try
            {
                if (File.Exists(_tokenPath))
                {
                    var json = File.ReadAllText(_tokenPath);
                    using var doc = JsonDocument.Parse(json);
                    _accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? "";
                    _refreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? "";
                }
            }
            catch { }
        }

        private void SaveTokens()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_tokenPath)!);
                var json = JsonSerializer.Serialize(new
                {
                    access_token = _accessToken,
                    refresh_token = _refreshToken
                });
                File.WriteAllText(_tokenPath, json);
            }
            catch { }
        }

        private async Task RefreshAccessTokenAsync()
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _refreshToken)
            });

            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", body);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? "";
            SaveTokens();
        }

        public string GetAuthUrl()
        {
            var scopes = "user-read-playback-state user-modify-playback-state user-read-currently-playing";
            return $"https://accounts.spotify.com/authorize?client_id={_clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(_redirectUri)}&scope={Uri.EscapeDataString(scopes)}";
        }

        public async Task ExchangeCodeAsync(string code)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri)
            });

            var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", body);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? "";
            _refreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? "";
            SaveTokens();
        }

        private void SetBearerToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        private async Task EnsureSpotifyOpenAsync()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("Spotify");
                if (processes.Length == 0)
                {
                    var spotifyPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Spotify", "Spotify.exe");
                    if (File.Exists(spotifyPath))
                    {
                        System.Diagnostics.Process.Start(spotifyPath);
                        await Task.Delay(3000);
                    }
                }
            }
            catch { }
        }

        public async Task PlayAsync(string? query = null)
        {
            await EnsureSpotifyOpenAsync();
            SetBearerToken();

            if (!string.IsNullOrEmpty(query))
            {
                var searchUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1";
                var searchResponse = await _httpClient.GetAsync(searchUrl);

                if (searchResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await RefreshAccessTokenAsync();
                    SetBearerToken();
                    searchResponse = await _httpClient.GetAsync(searchUrl);
                }

                var searchJson = await searchResponse.Content.ReadAsStringAsync();
                using var searchDoc = JsonDocument.Parse(searchJson);
                var uri = searchDoc.RootElement
                    .GetProperty("tracks")
                    .GetProperty("items")[0]
                    .GetProperty("uri")
                    .GetString();

                var playBody = JsonSerializer.Serialize(new { uris = new[] { uri } });
                await _httpClient.PutAsync("https://api.spotify.com/v1/me/player/play",
                    new StringContent(playBody, Encoding.UTF8, "application/json"));
            }
            else
            {
                await _httpClient.PutAsync("https://api.spotify.com/v1/me/player/play", null);
            }
        }

        public async Task PauseAsync()
        {
            SetBearerToken();
            await _httpClient.PutAsync("https://api.spotify.com/v1/me/player/pause", null);
        }

        public async Task NextAsync()
        {
            SetBearerToken();
            await _httpClient.PostAsync("https://api.spotify.com/v1/me/player/next", null);
        }

        public async Task PreviousAsync()
        {
            SetBearerToken();
            await _httpClient.PostAsync("https://api.spotify.com/v1/me/player/previous", null);
        }

        public async Task SetVolumeAsync(int volumePercent)
        {
            SetBearerToken();
            await _httpClient.PutAsync($"https://api.spotify.com/v1/me/player/volume?volume_percent={volumePercent}", null);
        }

        public async Task<string> GetCurrentTrackAsync()
        {
            SetBearerToken();
            var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
            if (!response.IsSuccessStatusCode) return "Nothing playing.";
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json)) return "Nothing playing.";
            using var doc = JsonDocument.Parse(json);
            var track = doc.RootElement.GetProperty("item").GetProperty("name").GetString();
            var artist = doc.RootElement.GetProperty("item").GetProperty("artists")[0].GetProperty("name").GetString();
            return $"{track} by {artist}";
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    }
}