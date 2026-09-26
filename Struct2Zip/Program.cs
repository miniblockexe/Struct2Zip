using Struct2Zip.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Struct2Zip API", Version = "v1" }));

builder.Services.AddScoped<ITreeParser, TreeParser>();
builder.Services.AddScoped<IZipBuilder, ZipBuilder>();
builder.Services.AddScoped<ITreeVisualizer, TreeVisualizer>();

var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:4200" };

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