using Models;
using System.ComponentModel.DataAnnotations;

namespace DiscordAutoPoster.Models
{
    public class AutoPost
    {
        [Key]
        public int Id { get; init; }

        [Required]
        public ApplicationUser Owner { get; init; } = null!;

        [MinLength(1)]
        [MaxLength(100)]
        [Required]
        public string Username { get; init; } = null!;

        [MinLength(1)]
        [MaxLength(1000)]
        [Required]
        public string Description { get; init; } = null!;

        [Required]
        public string Server { get; init; } = null!;
        
        [Required]
        public ulong ChannelId { get; init; }

        [Required]
        public DateTime LastTimePosted { get; set; }

        public string[]? ImagesUrl { get; set; }

        public bool Completed => ImagesUrl is not null;
    }
}
