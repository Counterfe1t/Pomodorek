﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Pomodorek.Platforms.Android.Receivers;
using Pomodorek.Platforms.Android.Services;

namespace Pomodorek.Services;

[Service]
public class TimerService : Service, ITimerService
{
    private CancellationTokenSource _token;

    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IDateTimeService _dateTimeService;

    public TimerService()
    {
        _notificationService = ServiceHelper.GetService<INotificationService>();
        _settingsService = ServiceHelper.GetService<ISettingsService>();
        _dateTimeService = ServiceHelper.GetService<IDateTimeService>();

        _token = new CancellationTokenSource();
    }

    public void Start(Action callback)
    {
        StartForegroundService();
        SetAlarm();

        var token = _token;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(Constants.OneSecondInMs);

                if (token.IsCancellationRequested)
                    return;

                callback?.Invoke();
            }
        });
    }

    public void Stop(bool isStoppedManually)
    {
        if (isStoppedManually)
            CancelAlarm();

        StopForegroundService();

        Interlocked.Exchange(ref _token, new CancellationTokenSource()).Cancel();
    }

    public override IBinder OnBind(Intent intent)
    {
        throw new NotImplementedException();
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        DisplayProgressNotification();

        return StartCommandResult.NotSticky;
    }

    private void StartForegroundService()
    {
        var startIntent = new Intent(MainActivity.ActivityCurrent, typeof(TimerService));
        MainActivity.ActivityCurrent.StartService(startIntent);
    }

    private void StopForegroundService()
    {
        var stopIntent = new Intent(MainActivity.ActivityCurrent, typeof(TimerService));
        MainActivity.ActivityCurrent.StopService(stopIntent);
    }

    private void DisplayProgressNotification()
    {
        var serializedNotification = _settingsService.Get(nameof(NotificationModel), string.Empty);
        var notification = JsonSerializer.Deserialize<NotificationModel>(serializedNotification);
        
        notification.Content = Constants.Messages.TimerIsRunning;
        notification.IsOngoing = true;
        notification.OnlyAlertOnce = true;
        notification.TriggerAlarmAt = notification.TriggerAlarmAt.AddSeconds(-1);
        StartForeground(notification.Id, Services.NotificationService.BuildNotification(notification));

        Task.Run(async () =>
        {
            var token = _token;
            var secondsRemaining = (int)notification.TriggerAlarmAt.Subtract(_dateTimeService.UtcNow).TotalSeconds;

            while (secondsRemaining > 0 && !token.IsCancellationRequested)
            {
                await Task.Delay(Constants.OneSecondInMs);

                notification.Content = TimeConverter.FormatTime(secondsRemaining);
                notification.CurrentProgress = secondsRemaining;
                StartForeground(notification.Id, Services.NotificationService.BuildNotification(notification));

                secondsRemaining = (int)notification.TriggerAlarmAt.Subtract(_dateTimeService.UtcNow).TotalSeconds;
            }
        });
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void SetAlarm()
    {
        var serializedNotification = _settingsService.Get(nameof(NotificationModel), string.Empty);
        var notification = JsonSerializer.Deserialize<NotificationModel>(serializedNotification);
        var triggerAlarmAtMs = (long)notification
            .TriggerAlarmAt
            .ToUniversalTime()
            .Subtract(_dateTimeService.UnixEpoch)
            .TotalMilliseconds;

        var intent = new Intent(MainActivity.ActivityCurrent, typeof(AlarmReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(MainActivity.ActivityCurrent, 1, intent, PendingIntentFlags.Immutable);
        var alarmManager = (AlarmManager)MainActivity.ActivityCurrent.GetSystemService(AlarmService);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAlarmAtMs, pendingIntent);
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            alarmManager.SetExact(AlarmType.RtcWakeup, triggerAlarmAtMs, pendingIntent);
    }

    private void CancelAlarm()
    {
        var intent = new Intent(MainActivity.ActivityCurrent, typeof(AlarmReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(MainActivity.ActivityCurrent, 1, intent, PendingIntentFlags.Immutable);

        var alarmManager = (AlarmManager)MainActivity.ActivityCurrent.GetSystemService(AlarmService);
        alarmManager.Cancel(pendingIntent);
    }
}