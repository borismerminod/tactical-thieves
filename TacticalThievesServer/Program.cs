using Fido2NetLib;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using TacticalThievesServer.Data;
using TacticalThievesServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Services
// =======================

// Charger le .env
Env.Load(); // lit le fichier .env à la racine

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");

if (string.IsNullOrEmpty(jwtKey))
{
    if (!builder.Environment.IsEnvironment("Testing"))
        throw new Exception("JWT_KEY n'est pas défini dans le .env");
    jwtKey = "test-jwt-key-for-testing-purposes-32chars!";
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddHttpContextAccessor();

string ServerDomain = "localhost";
//string ServerDomain = "mozell-fortifiable-moshe.ngrok-free.dev"; 

string ServerName = "TacticalThievesServer";
string Origin = "https://localhost:7186";
//string Origin = "https://mozell-fortifiable-moshe.ngrok-free.dev
//string Origin = "https://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins(Origin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton(new Fido2(new Fido2Configuration
{
    ServerDomain = ServerDomain,
    ServerName = ServerName,
    Origins = new HashSet<string> { Origin }
}));

builder.Services.AddSingleton<IWebSocketHandler, WebSocketHandler>();
builder.Services.AddSingleton<WebSocketLinkerService>();

builder.Services.AddControllers();

builder.Services.AddDistributedMemoryCache(); //Pour stocker les options FIDO2 entre les requêtes
builder.Services.AddSession(); //Pour stocker les options FIDO2 entre les requêtes

builder.Services.AddSignalR();



var dbPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
if (string.IsNullOrEmpty(dbPassword) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new Exception("SA_PASSWORD n'est pas défini dans le .env");
}
dbPassword ??= "test";

string? connectionStringWithoutPassword = builder.Configuration.GetConnectionString("DefaultConnection");

string finalConnectionString = (connectionStringWithoutPassword == null) ? "" : connectionStringWithoutPassword.Replace("{DB_PASSWORD}", dbPassword);

// <-- Ajout : enregistrement du DbContext pour SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(finalConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Appliquer les migrations au démarrage (créera la base si nécessaire)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =======================
// Pipeline HTTP
// =======================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT : Static Files AVANT TOUT
var provider = new FileExtensionContentTypeProvider();

// Unity WebGL
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".data"] = "application/octet-stream";
provider.Mappings[".framework.js"] = "application/javascript";
provider.Mappings[".symbols.json"] = "application/json";


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

app.MapFallbackToFile("angular/index.html");

// =======================
// WebSockets
// =======================

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<IWebSocketHandler>();
    await handler.HandleAsync(context);
});

// =======================
// Middleware classiques
// =======================

app.UseHttpsRedirection();

app.UseCors("AllowAngularClient");
app.UseSession();
app.UseAuthentication(); // Attention avant Authorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<ClientHub>("/hub");

app.Run();

public partial class Program { }