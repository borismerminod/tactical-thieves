using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TacticalThievesServer.Models
{
    [Table("PlayerProgress")]
    public class PlayerProgress
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public uint CurrentLevel { get; set; }
    }
}