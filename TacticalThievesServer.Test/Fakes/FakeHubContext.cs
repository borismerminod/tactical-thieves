using Microsoft.AspNetCore.SignalR;
using TacticalThievesServer.Services;

namespace TacticalThievesServer.Test.Fakes
{
    public class FakeHubContext : IHubContext<ClientHub>
    {
        public string? LastClientId { get; private set; }
        public string? LastEventName { get; private set; }
        public object? LastPayload { get; private set; }

        public string? LastBroadcastEventName { get; private set; }
        public object? LastBroadcastPayload { get; private set; }

        private readonly FakeHubClients _clients;

        public FakeHubContext()
        {
            _clients = new FakeHubClients(this);
        }

        public IHubClients Clients => _clients;
        public IGroupManager Groups => throw new NotImplementedException();

        internal void Capture(string clientId, string eventName, object? payload)
        {
            LastClientId = clientId;
            LastEventName = eventName;
            LastPayload = payload;
        }

        internal void CaptureBroadcast(string eventName, object? payload)
        {
            LastBroadcastEventName = eventName;
            LastBroadcastPayload = payload;
        }
    }

    internal class FakeHubClients : IHubClients
    {
        private readonly FakeHubContext _ctx;
        public FakeHubClients(FakeHubContext ctx) => _ctx = ctx;

        public IClientProxy Client(string connectionId) => new FakeClientProxy(connectionId, _ctx);

        public IClientProxy All => new FakeBroadcastProxy(_ctx);
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy Group(string groupName) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    internal class FakeBroadcastProxy : IClientProxy
    {
        private readonly FakeHubContext _ctx;
        public FakeBroadcastProxy(FakeHubContext ctx) => _ctx = ctx;

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _ctx.CaptureBroadcast(method, args.Length > 0 ? args[0] : null);
            return Task.CompletedTask;
        }
    }

    internal class FakeClientProxy : IClientProxy
    {
        private readonly string _clientId;
        private readonly FakeHubContext _ctx;

        public FakeClientProxy(string clientId, FakeHubContext ctx)
        {
            _clientId = clientId;
            _ctx = ctx;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _ctx.Capture(_clientId, method, args.Length > 0 ? args[0] : null);
            return Task.CompletedTask;
        }
    }
}
