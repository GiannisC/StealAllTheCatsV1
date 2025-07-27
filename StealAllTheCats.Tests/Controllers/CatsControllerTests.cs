using Azure;
using Castle.Core.Logging;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StealAllTheCats.Controllers;
using StealAllTheCats.Data;
using StealAllTheCats.DTOs;
using StealAllTheCats.Entities;
using StealAllTheCats.Services;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;

namespace StealAllTheCats.Tests.Controllers
{
    public class CatsControllerTests
    {
        private readonly ICatFetcherJob _catFetcherJob;
        private readonly DBContext _context;
        private readonly CatsController _controller;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<CatsController> _logger;

        public CatsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DBContext(options);
            _catFetcherJob = Substitute.For<ICatFetcherJob>();

            // Add some cats and tags to the database
            var tagPlayful = new TagEntity { Name = "Playful" };
            var tagCalm = new TagEntity { Name = "Calm" };

            var cat1 = new CatEntity { CatId = "1", Width = 100, Height = 100, ImagePath = "url1", Created = DateTime.UtcNow };
            cat1.Tags.Add(tagPlayful);

            var cat2 = new CatEntity { CatId = "2", Width = 200, Height = 200, ImagePath = "url2", Created = DateTime.UtcNow };
            cat2.Tags.Add(tagCalm);

            _context.Cats.AddRange(cat1, cat2);
            _context.Tags.AddRange(tagPlayful, tagCalm);
            _context.SaveChanges();

            // Logger (using a dummy)
            _backgroundJobs = Substitute.For<IBackgroundJobClient>();
            _logger = Substitute.For<ILogger<CatsController>>();

            _controller = new CatsController(_catFetcherJob,_context, _backgroundJobs, _logger);
        }

        [Fact]
        public async Task GetCats_WithTagFilter_ReturnsOnlyMatchingCats()
        {
            // Act: Filter by tag "Playful"
            var result = await _controller.GetCats(tag: "Playful", page: 1, pageSize: 10);

            // Assert: Only cats with the "Playful" tag are returned
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetCatsResponseDTO>(okResult.Value);

            Assert.Single(response.Data);
            Assert.Contains(response.Data, c => c.CatId == "1");
        }

        [Fact]
        public async Task GetCats_WithPaging_ReturnsCorrectPage()
        {
            // Act: Page size 1, page 2 should return the second cat
            var result = await _controller.GetCats(tag: string.Empty, page: 2, pageSize: 1);

            // Assert: Only the second cat is returned
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetCatsResponseDTO>(okResult.Value);

            Assert.Single(response.Data);
            Assert.Contains(response.Data, c => c.CatId == "2");
        }
    }
}
