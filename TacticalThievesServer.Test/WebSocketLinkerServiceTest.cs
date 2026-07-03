using TacticalThievesServer.Models;
using TacticalThievesServer.Services;
using Xunit;

namespace TacticalThievesServer.Test
{
    public class WebSocketLinkerServiceTest
    {
        [Fact]
        public void AddOrUpdate_NewSession_CreatesLinkerWithGivenIds()
        {
            var service = new WebSocketLinkerService();

            service.AddOrUpdate("session1", unityGuid: "unity1", angularGuid: "angular1");

            var found = service.TryGet("session1", out var linker);
            Assert.True(found);
            Assert.NotNull(linker);
            Assert.Equal("session1", linker!.SessionID);
            Assert.Equal("unity1", linker.UnityGUID);
            Assert.Equal("angular1", linker.AngularGUID);
        }

        [Fact]
        public void AddOrUpdate_NewSessionWithOnlyUnityGuid_AngularGuidIsNull()
        {
            var service = new WebSocketLinkerService();

            service.AddOrUpdate("session1", unityGuid: "unity1");

            service.TryGet("session1", out var linker);
            Assert.Equal("unity1", linker!.UnityGUID);
            Assert.Null(linker.AngularGUID);
        }

        [Fact]
        public void AddOrUpdate_ExistingSession_UpdatesOnlyProvidedGuid()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1", angularGuid: "angular1");

            service.AddOrUpdate("session1", unityGuid: "unity2");

            service.TryGet("session1", out var linker);
            Assert.Equal("unity2", linker!.UnityGUID);
            Assert.Equal("angular1", linker.AngularGUID);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddOrUpdate_ExistingSessionWithEmptyOrNullGuid_DoesNotOverwriteExistingValue(string? unityGuid)
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1", angularGuid: "angular1");

            service.AddOrUpdate("session1", unityGuid: unityGuid);

            service.TryGet("session1", out var linker);
            Assert.Equal("unity1", linker!.UnityGUID);
            Assert.Equal("angular1", linker.AngularGUID);
        }

        [Fact]
        public void AddOrUpdate_CalledTwiceWithBothGuidsSeparately_LinksBothClients()
        {
            var service = new WebSocketLinkerService();

            service.AddOrUpdate("session1", unityGuid: "unity1");
            service.AddOrUpdate("session1", angularGuid: "angular1");

            service.TryGet("session1", out var linker);
            Assert.Equal("unity1", linker!.UnityGUID);
            Assert.Equal("angular1", linker.AngularGUID);
        }

        [Fact]
        public void TryGet_ExistingSessionId_ReturnsTrueAndLinker()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1");

            var found = service.TryGet("session1", out var linker);

            Assert.True(found);
            Assert.NotNull(linker);
            Assert.Equal("session1", linker!.SessionID);
        }

        [Fact]
        public void TryGet_UnknownSessionId_ReturnsFalseAndNullLinker()
        {
            var service = new WebSocketLinkerService();

            var found = service.TryGet("unknown", out var linker);

            Assert.False(found);
            Assert.Null(linker);
        }

        [Fact]
        public void GetByAngular_ExistingAngularGuid_ReturnsMatchingLinker()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", angularGuid: "angular1");

            var linker = service.GetByAngular("angular1");

            Assert.NotNull(linker);
            Assert.Equal("session1", linker!.SessionID);
        }

        [Fact]
        public void GetByAngular_UnknownAngularGuid_ReturnsNull()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", angularGuid: "angular1");

            var linker = service.GetByAngular("unknown");

            Assert.Null(linker);
        }

        [Fact]
        public void GetByAngular_MultipleSessions_ReturnsCorrectOneAmongMany()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", angularGuid: "angular1");
            service.AddOrUpdate("session2", angularGuid: "angular2");
            service.AddOrUpdate("session3", angularGuid: "angular3");

            var linker = service.GetByAngular("angular2");

            Assert.NotNull(linker);
            Assert.Equal("session2", linker!.SessionID);
        }

        [Fact]
        public void GetByUnity_ExistingUnityGuid_ReturnsMatchingLinker()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1");

            var linker = service.GetByUnity("unity1");

            Assert.NotNull(linker);
            Assert.Equal("session1", linker!.SessionID);
        }

        [Fact]
        public void GetByUnity_UnknownUnityGuid_ReturnsNull()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1");

            var linker = service.GetByUnity("unknown");

            Assert.Null(linker);
        }

        [Fact]
        public void Remove_ExistingSessionId_RemovesLinker()
        {
            var service = new WebSocketLinkerService();
            service.AddOrUpdate("session1", unityGuid: "unity1");

            service.Remove("session1");

            var found = service.TryGet("session1", out var linker);
            Assert.False(found);
            Assert.Null(linker);
        }

        [Fact]
        public void Remove_UnknownSessionId_DoesNotThrow()
        {
            var service = new WebSocketLinkerService();

            var exception = Record.Exception(() => service.Remove("unknown"));

            Assert.Null(exception);
        }
    }
}
