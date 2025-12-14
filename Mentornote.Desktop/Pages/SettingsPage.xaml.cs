using Mentornote.Desktop.DTO;
using Mentornote.Desktop.Services;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Mentornote.Desktop.Pages
{
    public partial class SettingsPage : Page
    {
        
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        // ----------------------------
        // LOAD SETTINGS
        // ----------------------------
        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = await ApiClient.Client
                    .GetFromJsonAsync<UserSettingsDto>("settings/getAppsettings");

                if (settings != null)
                {
                    LoadSettingsIntoUi(settings);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to load settings.\n{ex.Message}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // ----------------------------
        // SAVE SETTINGS
        // ----------------------------
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = ReadSettingsFromUi();

                var response = await ApiClient.Client.PutAsJsonAsync(
                    "settings/savesettings",
                    settings
                );

                response.EnsureSuccessStatusCode();

                System.Windows.MessageBox.Show(
                    "Settings saved successfully.",
                    "MentorNote",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to save settings.\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // ----------------------------
        // RESET TO DEFAULTS
        // ----------------------------
        private async void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var confirm = System.Windows.MessageBox.Show(
                "Reset all settings to default values?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
               
            try
            {
                var defaults = GetDefaultSettings();

                var response = await ApiClient.Client.PutAsJsonAsync(
                    "settings/savesettings",
                    defaults
                );

                response.EnsureSuccessStatusCode();

                LoadSettingsIntoUi(defaults);

                System.Windows.MessageBox.Show(
                    "Settings reset to defaults.",
                    "MentorNote",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to reset settings.\n{ex.Message}",
                    "Reset Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // ----------------------------
        // UI → DTO
        // ----------------------------
        private UserSettingsDto ReadSettingsFromUi()
        {
            return new UserSettingsDto
            {
                RecentUtteranceCount = (int)UtteranceSlider.Value,
                ResponseFormat = (ResponseFormat)ResponseFormatCombo.SelectedIndex,
                ResponseTone = (ResponseTone)ResponseToneCombo.SelectedIndex,
                ResumeUsage = (ResumeUsage)ResumeUsageCombo.SelectedIndex,
                Creativity = CreativitySlider.Value,
                Theme = ThemeCombo.SelectedIndex == 0 ? Theme.Dark : Theme.Light
            };
        }

        // ----------------------------
        // DTO → UI
        // ----------------------------
        private void LoadSettingsIntoUi(UserSettingsDto settings)
        {
            UtteranceSlider.Value = settings.RecentUtteranceCount;
            ResponseFormatCombo.SelectedIndex = (int)settings.ResponseFormat;
            ResponseToneCombo.SelectedIndex = (int)settings.ResponseTone;
            ResumeUsageCombo.SelectedIndex = (int)settings.ResumeUsage;
            CreativitySlider.Value = settings.Creativity;
            ThemeCombo.SelectedIndex = settings.Theme == Theme.Dark ? 0 : 1;
        }

        private static UserSettingsDto GetDefaultSettings()
        {
            return new UserSettingsDto
            {
                ResponseFormat = (ResponseFormat)1,        // Guided
                ResponseTone = 0,          // Professional
                ResumeUsage = 0,           // Relevant only
                Theme = 0,                 // Dark
                RecentUtteranceCount = 15,
                Creativity = 0.6
            };
        }
    }
}
