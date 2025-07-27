namespace StealAllTheCats.Services
{
    using Microsoft.EntityFrameworkCore;
    using StealAllTheCats.Data;
    using StealAllTheCats.Entities;
    using System.ComponentModel.DataAnnotations;
    using System.Net.Http.Json;

    public class CatFetcherJob : ICatFetcherJob
    {
        private readonly DBContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CatFetcherJob> _logger;

        public CatFetcherJob(DBContext db, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<CatFetcherJob> logger)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task FetchAndStoreCatsAsync()
        {
            _logger.LogInformation("Starting FetchAndStoreCatsAsync at {Time}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"));
            var client = _httpClientFactory.CreateClient();

            var apiKey = _config["CatApi:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("API Key is missing from configuration.");
                throw new InvalidOperationException("Missing API Key");
            }
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var apiUrl = _config["CatApi:ApiUrl"];
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                _logger.LogError("API URL is missing from configuration.");
                throw new InvalidOperationException("Missing API URL");
            }

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"{apiUrl}/images/search?limit=25&has_breeds=1");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed cats API search call");
                throw;
            }

            var results = await response.Content.ReadFromJsonAsync<List<CatApiResponse>>();
            if (results == null || !results.Any())
            {
                return;
            }

            //Get existing data from database to check for duplicates
            var existingCatIds = new HashSet<string>(
                await _db.Cats.Select(c => c.CatId).ToListAsync()
            );

            var existingTags = await _db.Tags.ToListAsync();
            var tagDictionary = existingTags.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            var newCats = new List<CatEntity>();
            var newTags = new List<TagEntity>();
            foreach (var result in results)
            {
                // Skip duplicate
                if (existingCatIds.Contains(result.Id))
                {
                    continue; 
                }

                var cat = new CatEntity
                {
                    CatId = result.Id,
                    Width = result.Width,
                    Height = result.Height,
                    ImagePath = result.Url,
                    Created = DateTime.UtcNow
                };

                var breed = result.Breeds.FirstOrDefault();
                if (breed != null && !string.IsNullOrWhiteSpace(breed.Temperament))
                {
                    var tagNames = breed.Temperament.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    foreach (var tagName in tagNames)
                    {
                        if (!tagDictionary.TryGetValue(tagName, out var tag))
                        {
                            tag = new TagEntity { Name = tagName };
                            newTags.Add(tag);
                            tagDictionary[tagName] = tag;
                        }

                        cat.Tags.Add(tag);
                    }
                }

                //validate the cat entity and skip if invalid
                if (!ValidateModel(cat, out var validationErrors))
                {
                    foreach (var error in validationErrors)
                    {
                        _logger.LogError("Validation failed {@Validation}", new { CatId = cat.CatId, Error = error.ErrorMessage });
                    }
                    continue;
                }

                newCats.Add(cat);
            }

            if (newCats.Any())
            {
                _db.Cats.AddRange(newCats);
            }

            if (newTags.Any())
            {
                _db.Tags.AddRange(newTags);
            }

            try
            {
                if(_db.ChangeTracker.HasChanges())
                {
                    _logger.LogInformation("Saving {CatCount} cats and {TagCount} tags", newCats.Count, newTags.Count);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed DB save");
                throw;
            }
        }


        private class CatApiResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public int Width { get; set; }
            public int Height { get; set; }
            public List<Breed> Breeds { get; set; } = new();
        }

        private class Breed
        {
            public string Temperament { get; set; } = "";
        }

        // Validate helper method
        private bool ValidateModel(object model, out List<ValidationResult> results)
        {
            var context = new ValidationContext(model);
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        }
    }

}
