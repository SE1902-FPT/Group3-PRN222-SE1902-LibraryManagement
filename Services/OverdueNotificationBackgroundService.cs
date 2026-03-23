namespace Group3_SE1902_PRN222_LibraryManagement.Services;

public sealed class OverdueNotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueNotificationBackgroundService> _logger;

    public OverdueNotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueNotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOverdueNotificationsAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOverdueNotificationsAsync(stoppingToken);
        }
    }

    private async Task ProcessOverdueNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IParentNotificationService>();
            var createdCount = await notificationService.CreateAndPushOverdueNotificationsAsync(cancellationToken);

            if (createdCount > 0)
            {
                _logger.LogInformation("Đã tạo {Count} thông báo quá hạn cho phụ huynh.", createdCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi chạy job kiểm tra sách quá hạn.");
        }
    }
}
