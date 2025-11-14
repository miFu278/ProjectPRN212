using Microsoft.EntityFrameworkCore;
using webAPI.Models;

namespace webAPI.Services
{
    public class UsersService
    {
        private readonly BatterySwapContext _db;
        public UsersService(BatterySwapContext db) => _db = db;

        private static bool IsBlank(string? s) => string.IsNullOrWhiteSpace(s);

        // AUTH / FETCH
        public async Task<Users?> CheckLoginAsync(string email, string password)
        {
            // (Giữ nguyên như Java: so sánh plain – nếu muốn hash, mình chỉnh sau)
            return await _db.Users
                .Where(u => u.Email == email && u.Password == password)
                .FirstOrDefaultAsync();
        }

        public Task<Users?> FindByIdAsync(int id) =>
            _db.Users.FirstOrDefaultAsync(u => u.ID == id);

        public Task<Users?> GetUserByEmailAsync(string email) =>
            _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public Task<bool> ExistsByEmailAsync(string email) =>
            _db.Users.AnyAsync(u => u.Email == email);

        public async Task<int> InsertUserAsync(Users u)
        {
            if (IsBlank(u.Status)) u.Status = "Active";
            if (IsBlank(u.Avatar_URL)) u.Avatar_URL = null;

            _db.Users.Add(u);
            await _db.SaveChangesAsync();
            return u.ID;
        }

        // UPDATE
        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string phone, string? avatarUrl)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null) return false;

            u.FullName = fullName;
            u.Phone = phone;
            u.Avatar_URL = IsBlank(avatarUrl) ? null : avatarUrl;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newHashedPassword)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null) return false;
            u.Password = newHashedPassword;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int userId, string status)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null) return false;
            u.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStationAsync(int userId, int? stationId)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null) return false;
            u.Station_ID = stationId;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAvatarAsync(int userId, string? avatarUrl)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null) return false;
            u.Avatar_URL= IsBlank(avatarUrl) ? null : avatarUrl;
            await _db.SaveChangesAsync();
            return true;
        }

        // UTIL
        public async Task<string?> GetUsernameByIdAsync(int userId) =>
            await _db.Users.Where(u => u.ID == userId).Select(u => u.FullName).FirstOrDefaultAsync();

        public async Task<int> GetStationIdByUserIdAsync(int userId)
        {
            var stationId = await _db.Users
                .Where(u => u.ID == userId)
                .Select(u => u.Station_ID)
                .FirstOrDefaultAsync();

            return stationId ?? -1;
        }

        public async Task<bool> UpdatePasswordByEmailAsync(string email, string newHashedPassword)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (u == null) return false;
            u.Password = newHashedPassword;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
