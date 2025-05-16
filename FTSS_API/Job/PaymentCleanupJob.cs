using Quartz;
using FTSS_API.Service.Interface;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FTSS_API.Jobs
{
    public class PaymentCleanupJob : IJob
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentCleanupJob> _logger;

        public PaymentCleanupJob(IPaymentService paymentService, ILogger<PaymentCleanupJob> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Bắt đầu chạy công việc hủy thanh toán hết hạn tại {Time}", System.DateTime.Now);

            try
            {
                var response = await _paymentService.CancelExpiredProcessingPayments();
                _logger.LogInformation("Kết quả công việc: {Message}, Dữ liệu: {@Data}", response.message, response.data);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi chạy công việc hủy thanh toán hết hạn");
            }

            _logger.LogInformation("Hoàn thành công việc hủy thanh toán hết hạn tại {Time}", System.DateTime.Now);
        }
    }
}