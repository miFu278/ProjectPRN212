using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webAPI.Models;

namespace webAPI.Services
{
    public class PackageService
    {
        private readonly BatterySwapContext _db;
        private readonly ILogger<PackageService> _logger;

        public PackageService(BatterySwapContext db, ILogger<PackageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Package>> GetAllActivePackagesAsync()
        {
            var cs = _db.Database.GetDbConnection().ConnectionString;
            _logger.LogInformation("[PackageService] ConnLen={len}", cs?.Length ?? 0);

            const string sql = @"
SELECT Package_ID, Name, Description, Price, Required_SoH, MinSoH, MaxSoH, Status
FROM dbo.[Package]
WHERE Status = 'active'
ORDER BY Package_ID DESC";

            return await _db.Package
                .FromSqlRaw(sql)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Package?> GetActivePackageByIdAsync(int id)
        {
            var cs = _db.Database.GetDbConnection().ConnectionString;
            _logger.LogInformation("[PackageService] ConnLen={len}", cs?.Length ?? 0);

            const string sql = @"
SELECT Package_ID, Name, Description, Price, Required_SoH, MinSoH, MaxSoH, Status
FROM dbo.[Package]
WHERE Package_ID = {0} AND Status = 'active'";

            return await _db.Package.FromSqlRaw(sql, id)
                      .AsNoTracking()
                      .FirstOrDefaultAsync();
        }
    }
}
