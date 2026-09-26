using Struct2Zip.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Struct2Zip API", Version = "v1" }));

builder.Services.AddScoped<ITreeParser,    TreeParser>();
builder.Services.AddScoped<IZipBuilder,    ZipBuilder>();
builder.Services.AddScoped<ITreeVisualizer, TreeVisualizer>();

// Feature 1: Import ZIP
builder.Services.AddScoped<IZipReader, ZipReader>();

// Feature 2: Namespace rename (manual prefix)
builder.Services.AddScoped<INamespaceScanner, NamespaceScanner>();
builder.Services.AddScoped<INamespaceRenamer, NamespaceRenamer>();

// Feature 3: Kéo-thả + tự động đổi namespace theo đường dẫn folder
builder.Services.AddScoped<IMoveAndDownloadService, MoveAndDownloadService>();

// Giới hạn upload multipart 20 MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(o =>
    o.AddPolicy("FrontendPolicy", p =>
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Struct2Zip v1"));
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();
