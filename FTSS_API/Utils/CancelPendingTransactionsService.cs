using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FTSS_API.Service.Interface;

namespace FTSS_API.Service
{
    public class CancelPendingTransactionsService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public CancelPendingTransactionsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var vnPayService = scope.ServiceProvider.GetRequiredService<IVnPayService>();
                    await vnPayService.CancelPendingTransactions(TimeSpan.FromMinutes(15));
                }

                // Chờ 5 phút trước khi chạy lại
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}