using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace StealAllTheCats.DTOs
{
    /// <summary>
    /// Represents a cat fetched from TheCatAPI.
    /// </summary>
    public class CatDTO
    {
        /// <summary>
        /// The unique ID of the cat record in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the image as returned by TheCatAPI.
        /// </summary>
        [Required]
        public string CatId { get; set; } = string.Empty;

        /// <summary>
        /// Width of the cat image in pixels.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Width { get; set; }

        /// <summary>
        /// Height of the cat image in pixels.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Height { get; set; }

        /// <summary>
        /// The URL of the cat image.
        /// </summary>
        [Required]
        [Url]
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// The UTC timestamp when the cat record was created in the database.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// List of temperament tags associated with the cat.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
   
}
