using Microsoft.EntityFrameworkCore;
using webAPI.Models;


namespace webAPI.Services
{
    public class VehicleService
    {
        private readonly BatterySwapContext _db;
        public VehicleService(BatterySwapContext db) => _db = db;

        // Lấy tất cả xe của user
        public async Task<List<Vehicle>> GetVehiclesByUserIdAsync(int userId)
        {
            return await _db.Vehicle
                .Where(v => v.User_ID == userId)
                .ToListAsync();
        }

        // Thêm xe mới
        public async Task<bool> InsertVehicleAsync(Vehicle v)
        {
            await _db.Vehicle.AddAsync(v);
            return await _db.SaveChangesAsync() > 0;
        }

        // Lấy 1 xe (bất kỳ) theo User_ID – nếu 1 user có nhiều xe, lấy cái đầu
        public async Task<Vehicle?> GetVehicleByUserIdAsync(int userId)
        {
            return await _db.Vehicle
                .Where(v => v.User_ID == userId)
                .FirstOrDefaultAsync();
        }

        // Tìm Model_ID theo tên model (Vehicle_Model.Model_Name)
        public async Task<int?> GetModelIdByNameAsync(string modelName)
        {
            modelName = modelName.Trim().ToLower();

            var m = await _db.Vehicle_Model
                .Where(x => x.Model_Name != null && x.Model_Name.ToLower() == modelName)
                .Select(x => (int?)x.Model_ID)
                .FirstOrDefaultAsync();

            return m;
        }

        // Kiểm tra VIN đã tồn tại chưa
        public async Task<bool> IsVinExistsAsync(string vin)
        {
            vin = vin.Trim().ToUpperInvariant();
            return await _db.Vehicle.AnyAsync(v => v.Vin == vin);
        }

        // Xoá xe theo Vehicle_ID
        public async Task<bool> DeleteVehicleByIdAsync(int vehicleId)
        {
            var v = await _db.Vehicle.FindAsync(vehicleId);
            if (v == null) return false;

            _db.Vehicle.Remove(v);
            return await _db.SaveChangesAsync() > 0;
        }

        // Check xem vehicleId có thuộc userId không
        public async Task<bool> UserOwnsVehicleAsync(int userId, int vehicleId)
        {
            return await _db.Vehicle
                .AnyAsync(v => v.Vehicle_ID == vehicleId && v.User_ID == userId);
        }

        // Lấy xe theo Vehicle_ID
        public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId)
        {
            return await _db.Vehicle
                .Where(v => v.Vehicle_ID == vehicleId)
                .FirstOrDefaultAsync();
        }

        // Lấy Model_ID từ Vehicle_ID
        public async Task<int?> GetModelIdByVehicleIdAsync(int vehicleId)
        {
            return await _db.Vehicle
                .Where(v => v.Vehicle_ID == vehicleId)
                .Select(v => (int?)v.Model_ID)
                .FirstOrDefaultAsync();
        }

        // Lấy Battery_Type từ Model_ID (Vehicle_Model.Battery_Type)
        public async Task<string?> GetBatteryTypeByModelIdAsync(int modelId)
        {
            return await _db.Vehicle_Model
                .Where(m => m.Model_ID == modelId)
                .Select(m => m.Battery_Type)
                .FirstOrDefaultAsync();
        }

        // Lấy Battery_Type từ Vehicle_ID (join Vehicle với Vehicle_Model)
        public async Task<string?> GetBatteryTypeByVehicleIdAsync(int vehicleId)
        {
            var query =
                from v in _db.Vehicle
                join m in _db.Vehicle_Model on v.Model_ID equals m.Model_ID
                where v.Vehicle_ID == vehicleId
                select m.Battery_Type;

            return await query.FirstOrDefaultAsync();
        }
    }
}
