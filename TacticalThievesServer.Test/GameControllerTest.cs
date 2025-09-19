using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;

namespace TacticalThievesServer.Test
{
    public class GameControllerTest : IClassFixture<WebApplicationFactory<Program>>
    {
        

        private readonly HttpClient _client;


        public GameControllerTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendMove_ShouldReturnSuccess()
        {
            var response = await _client.PostAsJsonAsync("/api/Game/move", new {});
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
        }

    }

    // DTO pour parser la réponse JSON
    public class GameControllerResponse
    {
        public bool Success { get; set; }
    }
}
