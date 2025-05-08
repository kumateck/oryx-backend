using APP.IRepository;

namespace APP.Services.Background;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class ApprovalEscalationService(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10); // Adjust interval as needed

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var approvalRepository = scope.ServiceProvider.GetRequiredService<IApprovalRepository>();
                try
                {
                    await approvalRepository.ProcessApprovalEscalations();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process approval escalations. Error: {e.Message}");
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}