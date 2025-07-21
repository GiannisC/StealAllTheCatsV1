using Swashbuckle.AspNetCore.Annotations;

namespace StealAllTheCats.DTOs
{
    /// <summary>
    /// Represents a paginated response containing a list of cat items.
    /// </summary>
  
    public class GetCatsResponseDTO
    {
        /// <summary>
        /// The total number of cats matching the filter.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// The current page number in the result set.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The list of cat items in the current page.
        /// </summary>
        public List<CatDTO> Data { get; set; } = new();
    }
}
