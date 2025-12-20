using Microsoft.AspNetCore.StaticFiles;
using TacticalThievesServer.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Services
// =======================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<ThiefStateService>();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =======================
// Pipeline HTTP
// =======================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ IMPORTANT : Static Files AVANT TOUT
var provider = new FileExtensionContentTypeProvider();

// Unity WebGL
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".data"] = "application/octet-stream";
provider.Mappings[".framework.js"] = "application/javascript";
provider.Mappings[".symbols.json"] = "application/json";

/*// Angular
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".mjs"] = "application/javascript";
provider.Mappings[".css"] = "text/css";
provider.Mappings[".json"] = "application/json";*/

/*// Angular
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".mjs"] = "application/javascript";
provider.Mappings[".css"] = "text/css";
provider.Mappings[".json"] = "application/json";
provider.Mappings[".html"] = "text/html";

// Unity WebGL files
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".data"] = "application/octet-stream";
provider.Mappings[".framework.js"] = "application/javascript";
provider.Mappings[".symbols.json"] = "application/octet-stream";*/

// (Option: if using compressed builds with fallback)
provider.Mappings[".unityweb"] = "application/octet-stream";

app.UseDefaultFiles(); // permet index.html
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        // Recommandé pour WebGL moderne
        ctx.Context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        ctx.Context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
    }
});

// =======================
// WebSockets
// =======================

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleAsync(context);
});

// =======================
// Middleware classiques
// =======================

//app.UseHttpsRedirection();

app.UseCors("AllowAngularClient");

app.UseAuthorization();

app.MapControllers();
app.MapHub<ClientHub>("/scorehub");

app.Run();

public partial class Program { }