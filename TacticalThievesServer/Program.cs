using Fido2NetLib;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using TacticalThievesServer.Data;
using TacticalThievesServer.Services;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Services
// =======================

var key = Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_123456");

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton(new Fido2(new Fido2Configuration
{
    ServerDomain = "localhost",
    ServerName = "TacticalThievesServer",
    Origins = new HashSet<string> {"https://localhost:4200" }
}));

builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<ThiefStateService>();

builder.Services.AddControllers();

builder.Services.AddDistributedMemoryCache(); //Pour stocker les options FIDO2 entre les requêtes
builder.Services.AddSession(); //Pour stocker les options FIDO2 entre les requêtes

builder.Services.AddSignalR();

// <-- Ajout : enregistrement du DbContext pour SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Appliquer les migrations au démarrage (créera la base si nécessaire)
using (var scope = app.Services.CreateScope())
{
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

app.UseHttpsRedirection();

app.UseCors("AllowAngularClient");
app.UseSession();
app.UseAuthentication(); // Attention avant Authorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<ClientHub>("/scorehub");

app.Run();

public partial class Program { }