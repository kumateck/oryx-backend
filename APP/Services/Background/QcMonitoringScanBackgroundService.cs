using APP.Services.QcWorksheets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

/// <summary>
/// Runs the QC monitoring due-date scan once a day.
/// <para>
/// A <see cref="BackgroundService"/> on a 24-hour delay, matching the convention every other
/// scheduled job in this codebase already follows (<see cref="MaterialBatchExpiryService"/>,
/// <see cref="LeaveExpiryService"/>, <see cref="ServiceExpiryService"/> and the rest). No
/// scheduler package is introduced for one job — there is none in the solution today, and adding
/// Hangfire or Quartz to schedule a single daily sweep would be a new infrastructure dependency
/// for no behaviour the existing pattern does not already give.
/// </para>
/// <para>
/// All the logic lives in <see cref="IQcMonitoringScanService"/>, which takes the date to scan as
/// a parameter. This class is the clock and nothing else, which is what lets the acceptance tests
/// exercise the real scan without a hosted process.
/// </para>
/// <para>
/// <b>System-raised, so no acting user.</b> The scan passes a null userId: the rounds it creates
/// were not raised by a person, and attributing them to one would be a fiction. This is the same
/// reason <c>TestRequest.ScheduleOrigin</c> distinguishes Scheduled from Unscheduled in the first
/// place — an unscheduled round always has a person and a reason behind it, a scheduled one has
/// the schedule.
/// </para>
/// </summary>
public class QcMonitoringScanBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scan = scope.ServiceProvider.GetRequiredService<IQcMonitoringScanService>();

                await scan.RunAsync(DateTime.UtcNow, null, stoppingToken);
            }
            catch (Exception e)
            {
                // Swallowed and logged the same way every other hosted job here handles a failed
                // pass: one bad night must not take the loop down, and tomorrow's run picks up
                // everything this one missed, because a program that was not generated was not
                // advanced either.
                Console.WriteLine(e);
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
