using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TacticalThievesServer.Models
{
    [Table("PlayerProgress")]
    public class PlayerProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Pseudo { get; set; } = null!;

        [Required]
        public int CurrentLevel { get; set; }
    }
}