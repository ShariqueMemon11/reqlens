using Microsoft.EntityFrameworkCore;
using ReqLens.Application.Documents;
using ReqLens.Application.Llm;
using ReqLens.Infrastructure.Llm;
using ReqLens.Infrastructure.Pdf;
using ReqLens.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = DocumentUploadService.MaxUploadBytes;
});

var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' is not configured. Set ConnectionStrings:Postgres via user secrets.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
builder.Services.AddScoped<IRequirementExtractionService, RequirementExtractionService>();
builder.Services.AddScoped<IAmbiguityDetectionService, AmbiguityDetectionService>();
builder.Services.AddScoped<IContradictionDetectionService, ContradictionDetectionService>();
builder.Services.AddScoped<IQualityScoringService, QualityScoringService>();
builder.Services.AddScoped<IIssueResolutionService, IssueResolutionService>();
builder.Services.Configure<GroqLlmOptions>(builder.Configuration.GetSection(GroqLlmOptions.SectionName));
builder.Services.Configure<GeminiEmbeddingOptions>(builder.Configuration.GetSection(GeminiEmbeddingOptions.SectionName));
builder.Services.Configure<ContradictionDetectionOptions>(
    builder.Configuration.GetSection(ContradictionDetectionOptions.SectionName));
builder.Services.AddHttpClient<ILlmClient, GroqLlmClient>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddHttpClient<IEmbeddingClient, GeminiEmbeddingClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
