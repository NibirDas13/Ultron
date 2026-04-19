using Ultron.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ClaudeService>();
builder.Services.AddSingleton<NewsService>();
builder.Services.AddSingleton<SpotifyService>();
builder.Services.AddSingleton<VoiceService>();
builder.Services.AddSingleton<WhisperService>();
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();