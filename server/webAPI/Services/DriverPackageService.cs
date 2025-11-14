using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webAPI.Models;

namespace webAPI.Services
{
    public class DriverPackageService
    {
        private readonly BatterySwapContext _db;
        private readonly ILogger<DriverPackageService> _logger;

        public DriverPackageService(BatterySwapContext db, ILogger<DriverPackageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int?> GetCurrentPackageIdAsync(int userId)
        {
            var csLen = _db.Database.GetDbConnection().ConnectionString?.Length ?? 0;
            _logger.LogInformation("[DriverPackageService.GetCurrentPackageId] ConnLen={len}", csLen);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var pkgId = await _db.DriverPackage
                .Where(x => x.User_ID == userId && (!x.End_date.HasValue || x.End_date.Value >= today))
                .OrderByDescending(x => x.End_date == null)
                .ThenByDescending(x => x.End_date)
                .Select(x => (int?)x.Package_ID)
                .FirstOrDefaultAsync();

            return pkgId;
        }

        public async Task<bool> ExistsAsync(int userId)
        {
            var csLen = _db.Database.GetDbConnection().ConnectionString?.Length ?? 0;
            _logger.LogInformation("[DriverPackageService.Exists] ConnLen={len}", csLen);

            return await _db.DriverPackage.AnyAsync(x => x.User_ID == userId);
        }

        public async Task<bool> UpdateAsync(int userId, int packageId, DateTime start, DateTime end)
        {
            var csLen = _db.Database.GetDbConnection().ConnectionString?.Length ?? 0;
            _logger.LogInformation("[DriverPackageService.Update] ConnLen={len}", csLen);

            var row = await _db.DriverPackage.FirstOrDefaultAsync(x => x.User_ID == userId);
            if (row == null) return false;

            row.Package_ID = packageId;
            row.Start_date = DateOnly.FromDateTime(start.Date);
            row.End_date = DateOnly.FromDateTime(end.Date);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> InsertAsync(int userId, int packageId, DateTime start, DateTime end)
        {
            var csLen = _db.Database.GetDbConnection().ConnectionString?.Length ?? 0;
            _logger.LogInformation("[DriverPackageService.Insert] ConnLen={len}", csLen);

            _db.DriverPackage.Add(new DriverPackage
            {
                User_ID = userId,
                Package_ID = packageId,
                Start_date = DateOnly.FromDateTime(start.Date),
                End_date = DateOnly.FromDateTime(end.Date)
            });

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
