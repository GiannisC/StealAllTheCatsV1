using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using StealAllTheCats.Data;
using StealAllTheCats.Entities;
using StealAllTheCats.Services;
using Xunit;

namespace StealAllTheCats.Tests.Services
{
    public class CatFetcherJobTests
    {
        [Fact]
        public async Task FetchAndStoreCatsAsync_AddsNewCats_WhenValid()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new DBContext(options);

            // Mock response data for a valid cat
            var responseData = new[]
            {
                new
                {
                    Id = "cat123",
                    Url = "https://cdn2.thecatapi.com/images/cat.jpg",
                    Width = 600,
                    Height = 400,
                    Breeds = new[] { new { Temperament = "Playful, Friendly" } }
                }
            };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://api.thecatapi.com/v1/images/search*")
                    .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(responseData));

            var httpClient = mockHttp.ToHttpClient();

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["CatApi:ApiKey"].Returns("test-key");
            config["CatApi:ApiUrl"].Returns("https://api.thecatapi.com/v1");

            var logger = Substitute.For<ILogger<CatFetcherJob>>();

            var job = new CatFetcherJob(context, factory, config, logger);

            // Act
            await job.FetchAndStoreCatsAsync();

            // Assert
            var cats = context.Cats.Include(c => c.Tags).ToList();
            Assert.Single(cats);
            Assert.Equal("cat123", cats[0].CatId);
            Assert.Equal(2, cats[0].Tags.Count);
            Assert.Contains(cats[0].Tags, t => t.Name == "Playful");
            Assert.Contains(cats[0].Tags, t => t.Name == "Friendly");
        }

        [Fact]
        public async Task FetchAndStoreCatsAsync_DoesNotAddCat_WhenValidationFails()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new DBContext(options);

            // Mock response data with invalid cat (CatId null or empty)
            var responseData = new[]
            {
                new
                {
                    Id = "", // invalid CatId
                    Url = "https://cdn2.thecatapi.com/images/invalid.jpg",
                    Width = 800,
                    Height = 600,
                    Breeds = new[]
                    {
                        new { Temperament = "Calm" }
                    }
                }
            };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://api.thecatapi.com/v1/images/search*")
                    .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(responseData));

            var httpClient = mockHttp.ToHttpClient();

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["CatApi:ApiKey"].Returns("test-key");
            config["CatApi:ApiUrl"].Returns("https://api.thecatapi.com/v1");

            var logger = Substitute.For<ILogger<CatFetcherJob>>();

            var job = new CatFetcherJob(context, factory, config, logger);

            // Act
            await job.FetchAndStoreCatsAsync();

            // Assert
            Assert.Empty(context.Cats); // no cats added

            // Verify logger warning called once for validation error
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(state => state.ToString().Contains("Validation failed")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()
            );
        }

        [Fact]
        public async Task FetchAndStoreCatsAsync_IgnoresDuplicateCats()
        {
            // Setup in-memory DB with an existing cat
            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            await using var context = new DBContext(options);

            var existingCat = new CatEntity
            {
                CatId = "cat123",
                Width = 100,
                Height = 100,
                ImagePath = "existing_url",
                Created = DateTime.UtcNow
            };
            context.Cats.Add(existingCat);
            await context.SaveChangesAsync();

            // Mock response from API containing the same cat (cat123) + a new one (cat456)
            var responseData = new[]
            {
                new
                {
                    Id = "cat123", // Duplicate
                    Url = "https://example.com/cat123.jpg",
                    Width = 600,
                    Height = 400,
                    Breeds = new[] { new { Temperament = "Playful" } }
                },
                new
                {
                    Id = "cat456", // New cat
                    Url = "https://example.com/cat456.jpg",
                    Width = 500,
                    Height = 350,
                    Breeds = new[] { new { Temperament = "Calm" } }
                }
            };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://api.thecatapi.com/v1/images/search*")
                    .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(responseData));

            var httpClient = mockHttp.ToHttpClient();

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["CatApi:ApiKey"].Returns("test-key");
            config["CatApi:ApiUrl"].Returns("https://api.thecatapi.com/v1");

            var logger = Substitute.For<ILogger<CatFetcherJob>>();

            var job = new CatFetcherJob(context, factory, config, logger);

            // Execute
            await job.FetchAndStoreCatsAsync();

            // Assert: The database contains exactly 2 cats
            var cats = context.Cats.Include(c => c.Tags).ToList();
            Assert.Equal(2, cats.Count);

            // The old cat exists
            Assert.Contains(cats, c => c.CatId == "cat123");
            // And the new cat was added
            Assert.Contains(cats, c => c.CatId == "cat456");
        }

        [Fact]
        public async Task FetchAndStoreCatsAsync_ThrowsOnHttpRequestFailure()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new DBContext(options);

            // Mock response with failure
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://api.thecatapi.com/v1/images/search*")
                    .Respond(HttpStatusCode.InternalServerError);

            var httpClient = mockHttp.ToHttpClient();

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

            var config = Substitute.For<IConfiguration>();
            config["CatApi:ApiKey"].Returns("test-key");
            config["CatApi:ApiUrl"].Returns("https://api.thecatapi.com/v1");

            var logger = Substitute.For<ILogger<CatFetcherJob>>();

            var job = new CatFetcherJob(context, factory, config, logger);

            // Act & Assert: The method should throw HttpRequestException
            await Assert.ThrowsAsync<HttpRequestException>(() => job.FetchAndStoreCatsAsync());

            // Verify that the logger was called for error
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(state => state.ToString().Contains("Failed cats API search call")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()
            );
        }
    }
}