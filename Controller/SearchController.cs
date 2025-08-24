using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly OpenSearchService _searchService;

        public SearchController(OpenSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<ActionResult<List<OpenSearchService.SearchResult>>> Search(
            [FromQuery] string query,
            [FromQuery] string key = null,
            [FromQuery] string value = null)
        {
            try
            {
                var connectionResult = await _searchService.TestConnectionAsync();
                bool isConnected = connectionResult.IsConnected;
                string errorMessage = connectionResult.ErrorMessage;

                if (!isConnected)
                {
                    return StatusCode(503, errorMessage ?? "OpenSearch service is unavailable");
                }

                if (string.IsNullOrEmpty(query) && (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)))
                {
                    return BadRequest("Query or key-value pair is required");
                }

                string searchQuery = !string.IsNullOrEmpty(query) ? query : $"{key}:{value}";
                var results = await _searchService.SearchDocumentsAsync(searchQuery);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Search failed: {ex.Message}");
            }
        }
    }
}
