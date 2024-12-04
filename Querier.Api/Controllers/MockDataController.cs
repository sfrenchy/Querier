using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/v1/queries")]
    [ApiController]
    public class MockDataController : ControllerBase
    {
        [HttpGet("recent")]
        public ActionResult<List<string>> GetRecentQueries()
        {
            // Données simulées
            var mockQueries = new List<string>
            {
                "SELECT * FROM users WHERE active = true",
                "UPDATE products SET stock = 0 WHERE id = 123",
                "INSERT INTO orders (customer_id, total) VALUES (456, 99.99)",
                "DELETE FROM cart WHERE expired = true",
                "SELECT COUNT(*) FROM logs WHERE level = 'ERROR'"
            };

            return Ok(mockQueries);
        }

        [HttpGet("stats")]
        public ActionResult<Dictionary<string, int>> GetQueryStats()
        {
            var mockStats = new Dictionary<string, int>
            {
                { "Total Queries", 150 },
                { "Successful", 142 },
                { "Failed", 8 },
                { "Average Time (ms)", 245 }
            };

            return Ok(mockStats);
        }

        [HttpGet("activity")]
        public ActionResult<List<Dictionary<string, object>>> GetActivityData()
        {
            var mockActivity = new List<Dictionary<string, object>>();
            
            // Simuler 7 jours d'activité
            for (int i = 0; i < 7; i++)
            {
                mockActivity.Add(new Dictionary<string, object>
                {
                    { "date", System.DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd") },
                    { "value", new System.Random().Next(10, 50) }
                });
            }

            return Ok(mockActivity);
        }
    }
} 