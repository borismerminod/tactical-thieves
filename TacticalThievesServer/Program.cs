using Microsoft.AspNetCore.StaticFiles;
using TacticalThievesServer.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
/*builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});*/

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200") // ton Angular
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // important pour SignalR
    });
});

builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ThiefStateService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".data"] = "application/octet-stream"; // <-- extension .data

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseStaticFiles();
app.UseDefaultFiles();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleAsync(context);
});

app.UseHttpsRedirection();

app.UseAuthorization();


app.UseCors("AllowAngularClient");
app.MapControllers();

app.MapHub<ClientHub>("/scorehub");

app.Run();

public partial class Program { }
