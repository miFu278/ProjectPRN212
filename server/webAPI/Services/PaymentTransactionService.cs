using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webAPI.Models;

namespace webAPI.Services
{
    public class PaymentTransactionService
    {
        private readonly BatterySwapContext _db;
        private readonly ILogger<PaymentTransactionService> _logger;

        public PaymentTransactionService(BatterySwapContext db, ILogger<PaymentTransactionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> InsertPaymentAsync(
            int userId, int? stationId, int? packageId,
            double amountDouble, string method, string description, DateTime at)
        {
            var csLen = _db.Database.GetDbConnection().ConnectionString?.Length ?? 0;
            _logger.LogInformation("[PaymentTransactionService.Insert] ConnLen={len}", csLen);

            var amount = Math.Round(Convert.ToDecimal(amountDouble), 2);

            var txn = new PaymentTransaction
            {
                User_ID = userId,
                Station_ID = stationId,
                Package_ID = packageId,
                Amount = amount,
                Payment_Method = method,
                Description = description ?? string.Empty,
                Transaction_Time = at
            };

            _db.PaymentTransaction.Add(txn);
            await _db.SaveChangesAsync();
            return txn.ID; // PK identity
        }
    }
}
