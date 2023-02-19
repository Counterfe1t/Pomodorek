﻿using System.Windows.Input;

namespace Pomodorek.ViewModels;

public class MainPageViewModel : BaseViewModel
{
    private int _seconds;
    private TimerStatusEnum _status = TimerStatusEnum.Stopped;
    private bool _isRunning = false;
    private int _sessionLength = 2;
    private int _sessionsElapsed;

    #region Properties

    public int Seconds
    {
        get => _seconds;
        set => SetProperty(ref _seconds, value);
    }

    public TimerStatusEnum Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    // TODO: Change property so it represents state of the timer (running, paused, stopped)
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public int SessionLength
    {
        get => _sessionLength;
        set => SetProperty(ref _sessionLength, value);
    }

    public int SessionsElapsed
    {
        get => _sessionsElapsed;
        set => SetProperty(ref _sessionsElapsed, value);
    }

    #endregion

    private readonly ITimerService _timerService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IConfigurationService _configurationService;
    private readonly ISoundService _soundService;

    private AppSettings AppSettings => _configurationService.GetAppSettings();

    public ICommand StartCommand { get; private set; }
    public ICommand StopCommand { get; private set; }

    public MainPageViewModel(
        ITimerService timerService,
        INotificationService notificationService,
        ISettingsService settingsService,
        IConfigurationService configurationService,
        ISoundService soundService)
    {
        Title = Constants.PageTitles.Pomodorek;
        _timerService = timerService;
        _notificationService = notificationService;
        _settingsService = settingsService;
        _configurationService = configurationService;
        _soundService = soundService;

        StartCommand = new Command(async () => await StartSession());
        StopCommand = new Command(StopSession);
    }

    public async Task StartSession()
    {
        // TODO: Handle pausing timer
        if (IsRunning)
            return;

        IsRunning = true;
        SessionsElapsed = 0;
        await _soundService.PlaySound(Constants.Sounds.SessionStart);
        SetTimer(TimerStatusEnum.Focus);
    }

    public void StopSession()
    {
        _timerService.Stop();
        Seconds = 0;
        IsRunning = false;
        Status = TimerStatusEnum.Stopped;
    }

    public async Task DisplayNotification(string message) =>
        await _notificationService.DisplayNotification(message);

    private void SetTimer(TimerStatusEnum status)
    {
        Status = status;
        Seconds = GetDurationInMin(status) * Constants.SixtySeconds;
        _timerService.Start(async () => await HandleOnTickEvent());
    }

    private async Task HandleOnTickEvent()
    {
        if (Seconds == 0)
        {
            _timerService.Stop();
            await HandleOnFinishedEvent();
            return;
        }
        Seconds--;
    }

    private async Task HandleOnFinishedEvent()
    {
        switch (Status)
        {
            case TimerStatusEnum.Focus:
                if (++SessionsElapsed >= SessionLength)
                {
                    StopCommand.Execute(null);
                    await DisplayNotification(Constants.NotificationMessages.SessionOver);
                    await _soundService.PlaySound(Constants.Sounds.SessionOver);
                    break;
                }

                if (SessionsElapsed % 4 == 0)
                {
                    await DisplayNotification(Constants.NotificationMessages.LongRest);
                    SetTimer(TimerStatusEnum.LongRest);
                    break;
                }

                await DisplayNotification(Constants.NotificationMessages.ShortRest);
                SetTimer(TimerStatusEnum.ShortRest);
                break;
            case TimerStatusEnum.ShortRest:
            case TimerStatusEnum.LongRest:
                await DisplayNotification(Constants.NotificationMessages.Focus);
                SetTimer(TimerStatusEnum.Focus);
                break;
            case TimerStatusEnum.Stopped:
            default:
                break;
        }
    }

    private int GetDurationInMin(TimerStatusEnum status) =>
        status switch
        {
            TimerStatusEnum.Focus => _settingsService.Get(
                Constants.Settings.FocusLengthInMin,
                AppSettings.DefaultFocusLengthInMin),
            TimerStatusEnum.ShortRest => _settingsService.Get(
                Constants.Settings.ShortRestLengthInMin,
                AppSettings.DefaultShortRestLengthInMin),
            TimerStatusEnum.LongRest => _settingsService.Get(
                Constants.Settings.LongRestLengthInMin,
                AppSettings.DefaultLongRestLengthInMin),
            _ => 0,
        };
}