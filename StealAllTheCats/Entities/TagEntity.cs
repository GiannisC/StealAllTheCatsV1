using System.ComponentModel.DataAnnotations;

namespace StealAllTheCats.Entities
{
    public class TagEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public ICollection<CatEntity> Cats { get; set; } = new List<CatEntity>();
    }
}