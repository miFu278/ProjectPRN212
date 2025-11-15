using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webAPI.Models;

namespace webAPI.Controllers
{
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly BatterySwapContext _db;

        public StationsController(BatterySwapContext db)
        {
            _db = db;
        }
        // GET: api/stations
        // Returns a simple list of stations. By default returns only active stations.
        [HttpGet("api/stations")]
        public async Task<IActionResult> GetStations([FromQuery] bool activeOnly = true)
        {
            try
            {
                var q = _db.Station.AsQueryable();
                if (activeOnly)
                    q = q.Where(s => s.IsActive);

                var list = await q
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        station_ID = s.Station_ID,
                        name = s.Name,
                        address = s.Address,
                        isActive = s.IsActive
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                // If the exception (or any inner exception) indicates the IsActive column
                // is missing (older DB schema), fall back to a simple ADO.NET query.
                var combined = ex.ToString();
                if (combined.IndexOf("Invalid column name", StringComparison.OrdinalIgnoreCase) >= 0
                    || combined.IndexOf("IsActive", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Fallback: query only existing columns via ADO.NET to remain compatible with older DB schema
                    var result = new List<object>();
                    var conn = _db.Database.GetDbConnection();
                    try
                    {
                        await conn.OpenAsync();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT Station_ID, Name, Address FROM Station ORDER BY Name";
                        using var rd = await cmd.ExecuteReaderAsync();
                        while (await rd.ReadAsync())
                        {
                            result.Add(new
                            {
                                station_ID = rd.GetInt32(rd.GetOrdinal("Station_ID")),
                                name = rd["Name"]?.ToString(),
                                address = rd["Address"]?.ToString(),
                                isActive = (bool?)null
                            });
                        }
                    }
                    finally
                    {
                        try { await conn.CloseAsync(); } catch { }
                    }

                    return Ok(result);
                }

                // If it's a different error, return problem with details for debugging
                return Problem(title: "Failed to query stations", detail: ex.ToString());
            }
        }
    }
}
