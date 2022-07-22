﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UFHT_Plugin.HotkeyCommands;
using UFHT_Plugin.UserSettings;

namespace UFHT_Plugin.SettingsWindow
{
    /// <summary>
    /// Interaction logic for HotkeyWindow.xaml
    /// </summary>
    public partial class HotkeyWindow : Window
    {
        private SettingsManager _settingsManager;
        private Settings _settings;

        private SolidColorBrush onFocus = Brushes.MediumAquamarine;
        private SolidColorBrush offFocus = Brushes.Black;

        private Key _settingsKey = Key.None;
        private ModifierKeys _settingsModifier = ModifierKeys.None;

        private Key _onTopKey = Key.None;
        private ModifierKeys _onTopModifier = ModifierKeys.None;

        private Key _opacityKey = Key.None;
        private ModifierKeys _opacityModifier = ModifierKeys.None;

        private Key _ssMapKey = Key.None;
        private ModifierKeys _ssMapModifier = ModifierKeys.None;


        public HotkeyWindow(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _settings = _settingsManager.UserSettings;

            Application.Current.Resources["SettingsContent"] = HotkeyToString(_settings.SettingsHotkey.HotkeyCombo);
            Application.Current.Resources["OpacityContent"] = HotkeyToString(_settings.OpacityHotKey.HotkeyCombo);
            Application.Current.Resources["OnTopContent"] = HotkeyToString(_settings.OnTopHotkey.HotkeyCombo);
            Application.Current.Resources["SSMapContent"] = HotkeyToString(_settings.SSMapHotkey.HotkeyCombo);
            Application.Current.Resources["ClickThruContent"] = HotkeyToString(_settings.ClickThruHotkey.HotkeyCombo);

            Application.Current.Resources["OpacityGlobal"] = _settings.OpacityGlobal;
            Application.Current.Resources["OnTopGlobal"] = _settings.OnTopGlobal;
            Application.Current.Resources["SSMapGlobal"] = _settings.SSMapGlobal;
            Application.Current.Resources["ClickThruGlobal"] = _settings.ClickThruGlobal;

            InitializeComponent();
        }

        private void Hotkey_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var t = sender as Label;

            t.BorderBrush = onFocus;
        }

        private void SettingsHotkeyLabel_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var t = sender as Label;

            t.BorderBrush = offFocus;
        }

        private void Hotkey_OnKeyDown(object sender, KeyEventArgs e)
        {
            var lbl = sender as Label;
            LabelToHotkey(lbl, e);
        }

        private void Hotkey_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Label)sender).Focus();
        }

        //Exit

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            //repeat code, prob should separate into helper
            if (ValidateKeyCombo(_settingsKey, _settingsModifier))
            {
                _settings.SettingsHotkey = new Hotkey((int)_settingsKey, (int)_settingsModifier);
            }

            if (ValidateKeyCombo(_onTopKey, _onTopModifier))
            {
                _settings.OnTopHotkey = new Hotkey((int)_onTopKey, (int)_onTopModifier);
            }

            if (ValidateKeyCombo(_opacityKey, _opacityModifier))
            {
                _settings.OpacityHotKey = new Hotkey((int)_opacityKey, (int)_opacityModifier);
            }

            if (ValidateKeyCombo(_ssMapKey, _ssMapModifier))
            {
                _settings.SSMapHotkey = new Hotkey((int)_ssMapKey, (int)_ssMapModifier);
            }

            _settings.OpacityGlobal = OpacityGlobalToggle.IsChecked ?? false;
            _settings.OnTopGlobal = OnTopGlobalToggle.IsChecked ?? false;
            _settings.SSMapGlobal = SSMapGlobalToggle.IsChecked ?? false;
            _settings.ClickThruGlobal = ClickThruGlobalToggle.IsChecked ?? false;

            //save hotkeys
            _settingsManager.SaveSettings();
            _settings.LoadKeyGestures();

            Trace.WriteLine(
                $"{HotkeyToString(_settings.SettingsHotkey.HotkeyCombo)}\n" +
                $" {HotkeyToString(_settings.OpacityHotKey.HotkeyCombo)}\n" +
                $"{HotkeyToString(_settings.OnTopHotkey.HotkeyCombo)} \n" +
                $"{HotkeyToString(_settings.SSMapHotkey.HotkeyCombo)}\n");

            Close();
        }

        private void HotkeyWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter || e.Key is Key.Escape)
            {
                Save_OnClick(null, null);
            }

            if ((e.Key == Key.System && e.OriginalSource is Label) || (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                e.Handled = true;
            }
        }

        //helpers

        #region Helpers

        private string HotkeyToString(KeyGesture kg)
        {
            return $"{Enum.GetName(typeof(ModifierKeys), kg.Modifiers)}+{Enum.GetName(typeof(Key), kg.Key)}";
        }

        private void LabelToHotkey(Label lbl, KeyEventArgs e)
        {
            var key = e.Key;
            var modifier = ModifierKeys.None;

            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                modifier = ModifierKeys.Alt;
                if (Keyboard.IsKeyDown(Key.NumPad2))
                {
                    Trace.WriteLine("num2");
                    Trace.WriteLine(e.SystemKey);
                    Trace.WriteLine(e.Key);
                }

            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                modifier = ModifierKeys.Control;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                modifier = ModifierKeys.Shift;
                Trace.WriteLine(key);
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                modifier = ModifierKeys.Alt;
                // Trace.WriteLine(key);
                key = e.SystemKey;
                if (Keyboard.IsKeyDown(Key.NumPad2))
                {
                    key = Key.NumPad2;
                }
                //Trace.WriteLine(key);
            }


            if (modifier == ModifierKeys.Alt || modifier ==  ModifierKeys.Control || modifier == ModifierKeys.Shift) //use bool instead?
            {
                SetRelevantModifierKey(lbl.Name, modifier);

                if (((int)key >= 34 && (int)key <= 69) || ((int)key >= 74 && (int)key <= 113))
                {
                    SetRelevantKey(lbl.Name, key);
                    //Trace.WriteLine($"{modifier} + {key}");
                    lbl.Content =
                        $"{Enum.GetName(typeof(ModifierKeys), modifier)}+{Enum.GetName(typeof(Key), key)}";
                }
                else
                {
                    //Trace.WriteLine($"{modifier} + {key}");
                    lbl.Content = $"{Enum.GetName(typeof(ModifierKeys), modifier)}+???";
                }
            }
        }

        private void SetRelevantKey(string name, Key key)
        {
            switch (name)
            {
                case "SettingsLabel":
                    _settingsKey = key;
                    break;
                case "OnTopLabel":
                    _onTopKey = key;
                    break;
                case "OpacityLabel":
                    _opacityKey = key;
                    break;
                case "SSMapLabel":
                    _ssMapKey = key;
                    break;
            }
        }

        private void SetRelevantModifierKey(string name, ModifierKeys modifier)
        {
            switch (name)
            {
                case "SettingsLabel":
                    _settingsModifier = modifier;
                    break;
                case "OnTopLabel":
                    _onTopModifier = modifier;
                    break;
                case "OpacityLabel":
                    _opacityModifier = modifier;
                    break;
                case "SSMapLabel":
                    _ssMapModifier = modifier;
                    break;
            }
        }

        private bool ValidateKeyCombo(Key k, ModifierKeys m)
        {
            if (m > 0 && (int)k > 0)
            {
                Trace.WriteLine($"{m}+{k}");
                return true;
            }
            else
            {
                Trace.WriteLine($"Invalid Key Combo: {m} + {k}");
                return false;
            }
        }
        #endregion

        private void Label_OnKeyUp(object sender, KeyEventArgs e)
        {
            var lbl = sender as Label;
            LabelToHotkey(lbl, e);
        }
    }
}
