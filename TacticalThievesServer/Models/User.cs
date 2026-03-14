namespace TacticalThievesServer.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }

        public List<StoredCredential> Credentials { get; set; } = new();
    }
}
