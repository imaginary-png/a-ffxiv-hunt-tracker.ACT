﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UFHT_Plugin.HotkeyCommands;
using UFHT_Plugin.UserControls;
using UFHT_Plugin.UserControls.InfoSection;
using UFHT_Plugin.UserSettings;
using untitled_ffxiv_hunt_tracker;
using untitled_ffxiv_hunt_tracker.Entities;
using untitled_ffxiv_hunt_tracker.ViewModels;

namespace UFHT_Plugin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Session _session;

        //side panel section
        private InfoSectionControl _listSectionMain;

        //main map section
        private MainMapControl _mainMap;

        //Settings window
        private SettingsWindow.SettingsWindow _settingsWindow;

        private ObservableCollection<Mob> _nearbyMobs;
        internal Mob priorityMob;
        private DateTime priorityMobLabelLastUpdate;


        private SettingsManager _settingsManager;
        private Settings _userSettings;

        private bool _isClickThru = false;

        private Task task;
        private CancellationTokenSource ct;

        public MainWindow()
        {
            _settingsManager = new SettingsManager();
            _userSettings = _settingsManager.UserSettings;
            _settingsManager.PropertyChanged += SettingsManagerOnPropertyChanged;

            Application.Current.Resources["ProgramWidth"] = _userSettings.DefaultSizeX;
            Application.Current.Resources["ProgramHeight"] = _userSettings.DefaultSizeY;


            Application.Current.Resources["_sidePanelStartingWidth"] = 0.0;


            Application.Current.Resources["PriorityMobTextVisibility"] = Visibility.Hidden;
            Application.Current.Resources["PriorityMobGridInnerVisibility"] = Visibility.Hidden;

            //Application.Current.Resources["PriorityMobTextColour"] = Brushes.Aquamarine;
            Application.Current.Resources["PriorityMobTextColour"] = Brushes.WhiteSmoke;
            /*Application.Current.Resources["PriorityMobGridBackground"] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#444444")); */
            Application.Current.Resources["PriorityMobGridBackground"] = Brushes.Transparent;

            Application.Current.Resources["PriorityMobTextNamFontSize"] = 20.0;
            Application.Current.Resources["PriorityMobTextCoordsFontSize"] = 20.0;
            Application.Current.Resources["PriorityMobTextRankFontSize"] = 35.0;
            Application.Current.Resources["PriorityMobTextHPPFontSize"] = 26.0;
            Application.Current.Resources["PriorityMobTTFontSize"] = 20.0;

            Application.Current.Resources["ProgramOpacity"] = 1.0;
            Application.Current.Resources["ProgramTopMost"] = false;

            InitializeComponent();
            _nearbyMobs = new ObservableCollection<Mob>();

            task = Task.Run(() =>
            {
                ct = new CancellationTokenSource();

                _session = Program.CreateSession(_userSettings.RefreshRate);
                _session.ToggleLogS(_userSettings.LogS);
                _session.ToggleSRankTTS(_userSettings.SRankTTS);
                _session.ToggleARankTTS(_userSettings.ARankTTS);
                _session.ToggleBRankTTS(_userSettings.BRankTTS);

                Dispatcher.Invoke(() =>
                 {
                   _listSectionMain = new InfoSectionControl(_session, _nearbyMobs);
                   _session.CurrentNearbyMobs.CollectionChanged += CurrentNearbyMobs_CollectionChanged;

                   _mainMap = new MainMapControl(_session, _settingsManager);

                   MainGrid2.Children.Add(_mainMap);
                   ListSection.Children.Add(_listSectionMain);

                   DataContext = _session;
               });
                _session.Start(ct);

            });
        }


        //info side panel stuff
        private async void CurrentNearbyMobs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            #region brokenModelID
            //BROKEN MODELID VERSION
            /* if (sender is ObservableCollection<Mob> mobCollection)
             {

                 Dispatcher.Invoke(() =>
                 {
                     //Application.Current.Resources["_nearbyMobs"] = _nearbyMobs;
                     var toRemove = new ObservableCollection<Mob>();
                     foreach (var m in _nearbyMobs)
                     {
                         if (mobCollection.FirstOrDefault(m1 => m1.ModelID == m.ModelID) == null)
                         {
                             toRemove.Add(m);
                         }
                     }

                     foreach (var m in toRemove)
                     {
                         _nearbyMobs.Remove(m);
                     }


                     foreach (var m in mobCollection)
                     {
                         if (_nearbyMobs.FirstOrDefault(m1 => m1.ModelID == m.ModelID) == null)
                         {
                             _nearbyMobs.Add(m);
                         }
                     }
                 });
             }*/
            #endregion

            if (sender is ObservableCollection<Mob> mobCollection)
            {

                await Dispatcher.InvokeAsync(() =>
                {
                    var toRemove = new ObservableCollection<Mob>();
                    foreach (var m in _nearbyMobs)
                    {
                        if (mobCollection.FirstOrDefault(m1 => m1.Name == m.Name) == null)
                        {
                            toRemove.Add(m);
                        }
                    }

                    foreach (var m in toRemove)
                    {
                        _nearbyMobs.Remove(m);
                    }


                    foreach (var m in mobCollection)
                    {
                        if (_nearbyMobs.FirstOrDefault(m1 => m1.Name == m.Name) == null)
                        {
                            _nearbyMobs.Add(m);
                        }
                    }

                    //setting priority mob
                    if (priorityMob != null)
                    {
                        if (_nearbyMobs.Count == 0 ||
                            _nearbyMobs.FirstOrDefault(m => m.Name == priorityMob.Name) == null)
                        {
                            priorityMob.UnregisterHandlers();
                            priorityMob = null;
                            Application.Current.Resources["PriorityMobTextVisibility"] = Visibility.Hidden;
                            Application.Current.Resources["PriorityMobGridInnerVisibility"] = Visibility.Hidden;
                        }
                    }

                    foreach (var m in _nearbyMobs)
                    {
                        if (priorityMob == null ||
                            (HuntRank)Enum.Parse(typeof(HuntRank), m.Rank) >
                            (HuntRank)Enum.Parse(typeof(HuntRank), priorityMob.Rank))
                        {
                            priorityMob = m;
                            priorityMob.PropertyChanged += PriorityMob_OnPropertyChanged;

                            Application.Current.Resources["PriorityMobTextRank"] = priorityMob.Rank;
                            Application.Current.Resources["PriorityMobTextName"] = priorityMob.Name;
                            Application.Current.Resources["PriorityMobTTText"] = priorityMob.Name;
                            Application.Current.Resources["PriorityMobTextCoords"] = priorityMob.Coordinates;
                            Application.Current.Resources["PriorityMobTextHPP"] = $"{priorityMob.HPPercentAsPercentage,0:0}%";

                            Application.Current.Resources["PriorityMobTextVisibility"] = Visibility.Visible;
                            Application.Current.Resources["PriorityMobGridInnerVisibility"] = Visibility.Visible;

                            Trace.WriteLine("priority mob new");
                            priorityMobLabelLastUpdate = DateTime.Now;
                        }
                    }

                });
            }
        }



        #region Event Handlers

        //set current info for priority mob - this seems to cause lag when gpu usage is high (e.g. ffxiv unchecked fps limit).
        //this program itself can spike up to 3-4% gpu usage when rendering these labels on my 1080.
        //everything is smooth when setting ffxiv refresh rate to 1/2 instead of 144 fps / unchecked. but then the game ain't as smooth.
        private async void PriorityMob_OnPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            /*Application.Current.Resources["PriorityMobTextRank"] = priorityMob.Rank;
            Application.Current.Resources["PriorityMobTextName"] = priorityMob.Name;
            Application.Current.Resources["PriorityMobTTText"] = priorityMob.Name;*/

            //can cause lag to refresh too quickly.
            /*if ((DateTime.Now - priorityMobLabelLastUpdate).TotalMilliseconds > 100)
            {
                Trace.WriteLine("too early");
                return;
            }

            Trace.WriteLine("okay!");

            priorityMobLabelLastUpdate = DateTime.Now;
            */

            var mob = o as Mob;

            await Task.Run(() =>
            {
                // Trace.WriteLine("updating coords");
                // Trace.WriteLine($"{PriorityMobCoords.Content.Equals(priorityMob.Coordinates)} - {PriorityMobCoords.Content} - {priorityMob.Coordinates}");

                if (_userSettings.UpdatePriorityMobCoordinates && ((DateTime.Now - priorityMobLabelLastUpdate).TotalMilliseconds > 100))

                {
                    Application.Current.Resources["PriorityMobTextCoords"] = priorityMob.Coordinates.ToString();
                    priorityMobLabelLastUpdate = DateTime.Now;
                }

                //Application.Current.Resources["PriorityMobTextCoords"] = priorityMob.Coordinates;
                
                
                

                // Trace.WriteLine("updating hp");
                //  Trace.WriteLine($"{PriorityMobHPP.Content.Equals(priorityMob.HPPercent)} - {PriorityMobHPP.Content} - {HPPasString}");
                Application.Current.Resources["PriorityMobTextHPP"] = $"{mob.HPPercentAsPercentage,0:0}%";
            });

        }


        //priority mob tool tip
        private void PriorityMobText_OnMouseMove(object sender, MouseEventArgs e)
        {
            PriorityMobTT.IsOpen = true;
            PriorityMobTT.VerticalOffset = PriorityMobGridInner.ActualHeight * .8;
        }

        private void PriorityMobText_OnMouseLeave(object sender, MouseEventArgs e)
        {
            PriorityMobTT.IsOpen = false;
        }

        //priority mob row - make top black bar close program on double click.
        private void PriorityMobTopBar_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                SystemCommands.CloseWindow(this);
            }
        }

        //make the whole window draggable
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        //settings updated
        private void SettingsManagerOnPropertyChanged(object sender, Settings e)
        {
            //close the side panel first, otherwise it messes with the resize.
            if (InfoGrid.Width > 0)
            {
                SidePanelToggle_Executed(null, null); //prob bad and should separate into own method but.., turn off side panel
            }

            _session.SetRefreshRate(_userSettings.RefreshRate);
            this.Width = _userSettings.DefaultSizeX;
            this.Height = _userSettings.DefaultSizeY;
            _session.ToggleLogS(_userSettings.LogS);
            _session.ToggleSRankTTS(_userSettings.SRankTTS);
            _session.ToggleARankTTS(_userSettings.ARankTTS);
            _session.ToggleBRankTTS(_userSettings.BRankTTS);
            GlobalHotkey.VerifyHotKeys(this);

            //other stuff like updating fonts, etc, in the future
        }

        #region BUTTON event handlers

        //side panel toggle -- not needed if using command
        private void SidePanelToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (InfoGrid.Width == 0)
            {
                InfoGrid.Width = 300;
                MainWindow1.Width += 300;


            }
            else
            {
                InfoGrid.Width = 0;
                MainWindow1.Width -= 300;

            }
        }

        //exit button
        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion


        #endregion


        #region Commands

        private void OnTop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnTop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!MainWindow1.Topmost)
            {
                Application.Current.Resources["ProgramTopMost"] = true;
            }
            else
            {
                Application.Current.Resources["ProgramTopMost"] = false;
            }
        }

        private void OpacityToggle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpacityToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Opacity > 0.99)
            {
                Application.Current.Resources["ProgramOpacity"] = _userSettings.Opacity;
            }
            else
            {
                Application.Current.Resources["ProgramOpacity"] = 1.0;
            }
        }

        private void SidePanelToggle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SidePanelToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (InfoGrid.Width == 0)
            {
                InfoGrid.Width = 300;
                MainWindow1.Width += 300;
            }
            else
            {
                InfoGrid.Width = 0;
                MainWindow1.Width -= 300;
            }
        }

        private void SSMapToggle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SSMapToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _session.ToggleSSMap();
        }

        private void SettingsWindowToggle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SettingsWindowToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            new SettingsWindow.SettingsWindow(_settingsManager) { Owner = this }.ShowDialog();
        }

        private void ClickThruToggle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ClickThruToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ClickThruToggle();
        }

        #endregion




        //////////// Global hotkey  - base code from https://stackoverflow.com/questions/11377977/global-hotkeys-in-wpf-working-from-every-window


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlobalHotkey.OnSourceInitialized(this, _userSettings);
        }

        protected override void OnClosed(EventArgs e)
        {
            GlobalHotkey.OnClosed(this);
            ct.Cancel();

            Application.Current.Dispatcher.InvokeShutdown();
            base.OnClosed(e);
        }


        //for global hotkeys
        public void OpacityToggle()
        {
            OpacityToggle_Executed(null, null);
        }
        public void OnTopToggle()
        {
            OnTop_Executed(null, null);
            //minimize if toggling OFF ontop, otherwise it can get stuck until user clicks on and off.
            if (!Topmost)
            {
                this.WindowState = WindowState.Minimized;
                if (_userSettings.ClickThruWhenOnTop)
                {
                    ClickThru.DisableMouseClickThru(this);
                    _isClickThru = false;
                }
            }
            else
            {
                this.WindowState = WindowState.Normal; //alternatively, could just toggle opacity to 0?
                if (_userSettings.ClickThruWhenOnTop)
                {
                    ClickThru.EnableMouseClickThru(this);
                    _isClickThru = true;
                }
            }
        }
        public void SSMapToggle()
        {
            SSMapToggle_Executed(null, null);
        }


        //click thru helper
        public void ClickThruToggle()
        {
            if (!_isClickThru)
            {
                ClickThru.EnableMouseClickThru(this);
                _isClickThru = !_isClickThru;
            }
            else
            {
                ClickThru.DisableMouseClickThru(this);
                _isClickThru = !_isClickThru;
            }
        }


        //override alt key menu press
        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {

            if ((e.Key == Key.System) || (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                if (e.SystemKey == Key.F4)
                {
                    this.Close();
                }
                e.Handled = true;
            }
        }


    }
}