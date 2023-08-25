using DiscordAutoPoster.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class ApplicationUser
    {
        [Key]
        public int Id { get; init; }

        public ulong DiscordId { get; init; }

        public DateTime? MutedUntil { get; set; }

        public int? CurrentAutoPostId { get; set; }
        [ForeignKey(nameof(CurrentAutoPostId))]
        public AutoPost? CurrentAutoPost { get; set; }
    }
}