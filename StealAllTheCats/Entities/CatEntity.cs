using System.ComponentModel.DataAnnotations;

namespace StealAllTheCats.Entities
{
    public class CatEntity
    {
        public int Id { get; set; }

        [Required]
        public string CatId { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Width { get; set; }

        [Range(1, int.MaxValue)]
        public int Height { get; set; }

        [Required]
        [Url]
        public string ImagePath { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public List<TagEntity> Tags { get; set; } = new List<TagEntity>();
    }

}
