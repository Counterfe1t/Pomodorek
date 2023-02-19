﻿namespace Pomodorek.Tests.UnitTests.ViewModels;

public class SettingsPageViewModelTests
{
    private readonly SettingsPageViewModel _viewModel;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;

    private static AppSettings AppSettings => new();

    public SettingsPageViewModelTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _configurationServiceMock = new Mock<IConfigurationService>();

        _viewModel = new SettingsPageViewModel(
            _settingsServiceMock.Object,
            _configurationServiceMock.Object);
    }

    [Fact]
    public void Initialize_WhenCalled_InitializesProperties()
    {
        // arrange
        _configurationServiceMock
            .Setup(x => x.GetAppSettings())
            .Returns(AppSettings);

        // act
        _viewModel.InitializeCommand.Execute(null);

        // assert
        _settingsServiceMock
            .Verify(x => x.Get(Constants.Settings.IsSoundEnabled, It.IsAny<bool>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Get(Constants.Settings.FocusLengthInMin, It.IsAny<int>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Get(Constants.Settings.ShortRestLengthInMin, It.IsAny<int>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Get(Constants.Settings.LongRestLengthInMin, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void SaveSettings_WhenCalled_SavesSettingsToStorage()
    {
        // arrange
        _configurationServiceMock
            .Setup(x => x.GetAppSettings())
            .Returns(AppSettings);

        // act
        _viewModel.SaveCommand.Execute(null);

        // assert
        _settingsServiceMock
            .Verify(x => x.Set(Constants.Settings.IsSoundEnabled, It.IsAny<bool>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Set(Constants.Settings.FocusLengthInMin, It.IsAny<int>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Set(Constants.Settings.ShortRestLengthInMin, It.IsAny<int>()), Times.Once);
        _settingsServiceMock
            .Verify(x => x.Set(Constants.Settings.LongRestLengthInMin, It.IsAny<int>()), Times.Once);
    }
}
