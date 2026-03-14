namespace TacticalThievesServer.Models
{
    public class StoredCredential
    {
        public int Id { get; set; }
        public byte[] DescriptorId { get; set; }
        public byte[] PublicKey { get; set; }
        public uint Counter { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
