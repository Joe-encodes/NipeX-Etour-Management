using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using e_tour_api.Data;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace e_tour_api.Tests
{
    public class DocumentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public DocumentsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with in-memory for testing
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });
        }

        private async Task<string> GetJwtTokenAsync(HttpClient client)
        {
            var loginData = new
            {
                Username = "testuser",
                Password = "testpassword"
            };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/auth/login", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("token").GetString() ?? "";
        }

        [Fact]
        public async Task UploadDocument_ReturnsOk()
        {
            var client = _factory.CreateClient();

            // Authenticate
            var token = await GetJwtTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Dummy PDF content"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", "test.pdf");
            formData.Add(new StringContent("Test Document"), "title");

            var response = await client.PostAsync("/api/documents/upload", formData);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Additional tests for approve and download can be added similarly
    }
}
