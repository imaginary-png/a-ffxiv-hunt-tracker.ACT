﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UFHT_Plugin.UserSettings;

namespace UFHT_Plugin.SettingsWindow
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsManager _settingsManager;
        private Settings _settings;

        public SettingsWindow(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _settings = _settingsManager.UserSettings;

            Application.Current.Resources["RefreshRateText"] = ((int)(_settings.RefreshRate)).ToString();
            Application.Current.Resources["OpacityText"] = ((int)(_settings.Opacity * 100)).ToString();
            Application.Current.Resources["StartingHeightText"] = ((int)_settings.DefaultSizeY).ToString();
            Application.Current.Resources["StartingWidthText"] = ((int)_settings.DefaultSizeX).ToString();
            Application.Current.Resources["MobIconText"] = ((int)_settings.MobIconSize).ToString();
            Application.Current.Resources["PlayerIconText"] = ((int)_settings.PlayerIconSize).ToString();
            Application.Current.Resources["LogS"] = _settings.LogS;
            Application.Current.Resources["SRankTTS"] = _settings.SRankTTS;
            Application.Current.Resources["ARankTTS"] = _settings.ARankTTS;
            Application.Current.Resources["BRankTTS"] = _settings.BRankTTS;
            Application.Current.Resources["ClickThruWhenOnTop"] = _settings.ClickThruWhenOnTop;
            Application.Current.Resources["UpdatePriorityMobCoordinates"] = _settings.UpdatePriorityMobCoordinates;

            InitializeComponent();
        }

        private void TextBox_OnPreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            var textbox = (TextBox)sender;

            Regex regex = new Regex(@"^(?:\d{1})$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void StartingHeightTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (StartingWidthTextBox != null)
            {
                StartingWidthTextBox.Text = textbox.Text;
            }
        }

        private void StartingWidthTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (StartingHeightTextBox != null)
            {
                StartingHeightTextBox.Text = textbox.Text;
            }
        }



        //Save Button
        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            int refresh;
            Double opacity, height, width, mobIcon, playerIcon;

            if (Int32.TryParse(RefreshRateTextBox.Text, out refresh))
            {
                _settings.RefreshRate = refresh;
            }
            
            if (Double.TryParse(OpacityTextBox.Text, out opacity))
            {
                _settings.Opacity = opacity / 100.0;
            }

            if (Double.TryParse(StartingHeightTextBox.Text, out height))
            {
                _settings.DefaultSizeY = height;
                _settings.DefaultSizeX = height;
            }

            if (Double.TryParse(StartingWidthTextBox.Text, out width))
            {
                _settings.DefaultSizeX = height;
                _settings.DefaultSizeY = height;
            }

            if (Double.TryParse(MobIconTextBox.Text, out mobIcon))
            {
                _settings.MobIconSize = mobIcon;
            }

            if (Double.TryParse(PlayerIconTextBox.Text, out playerIcon))
            {
                _settings.PlayerIconSize = playerIcon;
            }

            _settings.LogS = LogSRanks.IsChecked ?? false;
            _settings.SRankTTS = SRankTTSCHK.IsChecked ?? false;
            _settings.ARankTTS = ARankTTSCHK.IsChecked ?? false;
            _settings.BRankTTS = BRankTTSCHK.IsChecked ?? false;
            _settings.ClickThruWhenOnTop = ClickThruWhenOnTopCHK.IsChecked ?? false;
            _settings.UpdatePriorityMobCoordinates = UpdatePriorityMobCoordinatesCHK.IsChecked ?? false;

            _settingsManager.SaveSettings();

            Close();
        }

        private void SettingsWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                Save_OnClick(null, null); //dodgy?
            }
        }

        private void HotkeyEdit_OnClick(object sender, RoutedEventArgs e)
        {
            new HotkeyWindow(_settingsManager) { Owner = this }.ShowDialog();
        }
    }
}
