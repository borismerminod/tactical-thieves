namespace TacticalThievesServer.Models
{
    /// <summary>
    /// Represents a persisted FIDO2/WebAuthn credential linked to a user.
    /// Stores the public key, credential id (raw), and the signature counter used
    /// to detect cloned authenticators.
    /// </summary>
    public class StoredCredential
    {
        public int Id { get; set; }

        /// <summary>
        /// Raw credential identifier returned by the authenticator.
        /// </summary>
        public required byte[] DescriptorId { get; set; }

        /// <summary>
        /// Public key data associated with the credential.
        /// </summary>
        public required byte[] PublicKey { get; set; }

        /// <summary>
        /// Signature counter used to mitigate replay attacks.
        /// </summary>
        public uint Counter { get; set; }

        /// <summary>
        /// Foreign key to the owning user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property to the owning user.
        /// </summary>
        public User User { get; set; } = null!;
    }
}
