﻿using Sharlayan.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using untitled_ffxiv_hunt_tracker.Entities;
using untitled_ffxiv_hunt_tracker.Factories;
using untitled_ffxiv_hunt_tracker.Memory;
using untitled_ffxiv_hunt_tracker.Utilities;

namespace untitled_ffxiv_hunt_tracker.ViewModels
{
    public class Session
    {
        private readonly Dictionary<HuntRank, List<Mob>> ARRDict;
        private readonly Dictionary<HuntRank, List<Mob>> HWDict;
        private readonly Dictionary<HuntRank, List<Mob>> SBDict;
        private readonly Dictionary<HuntRank, List<Mob>> ShBDict;
        private readonly Dictionary<HuntRank, List<Mob>> EWDict;
        private readonly Dictionary<String, List<Mob>> Trains;

        private MemoryReader _memoryReader;
        private SpeechSynthesizer _tts;

        private bool _ssMap;
        private bool _logS;
        private bool _SRankTTS;
        private bool _ARankTTS;
        private bool _BRankTTS;

        private int _refreshRate;

        public List<Mob> CurrentTrain;
        private string CurrentTrainName { get; set; }

        public ObservableCollection<Mob> CurrentNearbyMobs { get; }
        public Player CurrentPlayer { get; set; }



        public Session(MemoryReader memoryReader, SpeechSynthesizer tts, int refreshRate)
        {
            _memoryReader = memoryReader;
            _tts = tts;
            _refreshRate = refreshRate;

            SetUpMemReader();

            ARRDict = ARRMobFactory.GetARRDict();
            HWDict = HWMobFactory.GetHWDict();
            SBDict = SBMobFactory.GetSBDict();
            ShBDict = ShBMobFactory.GetShBDict();
            EWDict = EWMobFactory.GetEWDict();

            _ssMap = false;
            _logS = false;
            _SRankTTS = false;
            _ARankTTS = false;
            _BRankTTS = false;

            Trains = new Dictionary<string, List<Mob>>();

            CurrentTrain = new List<Mob>();
            CurrentTrainName = null;

            CurrentNearbyMobs = new ObservableCollection<Mob>();
            CurrentNearbyMobs.CollectionChanged += LogSRanks;
            CurrentNearbyMobs.CollectionChanged += SRankTTS;
            CurrentNearbyMobs.CollectionChanged += ARankTTS;
            CurrentNearbyMobs.CollectionChanged += BRankTTS;

            CurrentPlayer = new Player();
            GetUser();
        }

        private void SetUpMemReader()
        {
            var processExists = _memoryReader.Setup();

            while (!processExists)
            {
                Console.WriteLine("Process not found... Trying again?");
                Trace.WriteLine("Process not found... Trying again..?");
                processExists = _memoryReader.Setup();
                Thread.Sleep(100);
            }
            Console.WriteLine("Process found!");
            Trace.WriteLine("Process found!");
        }

        public void Start(CancellationTokenSource ct)
        {
            Trace.WriteLine($"can get process? {_memoryReader.CanGetProcess} has exited? {_memoryReader.ProcessHasExited}");
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                //Trace.WriteLine($"1inside loop can get process? {_memoryReader.CanGetProcess} has exited? {_memoryReader.ProcessHasExited}");
                //this is really messy but it works. so...
                if (!_memoryReader.CanGetProcess || _memoryReader.ProcessHasExited)
                {
                    Trace.WriteLine(
                        $"1inside loop can get process? {_memoryReader.CanGetProcess} has exited? {_memoryReader.ProcessHasExited}");
                    Thread.Sleep(2000);
                    SetUpMemReader();
                }

                GetUser();

                //if not in a valid hunt map, only refresh every 250ms. Skip searching for mobs.
                if (!Enum.IsDefined(typeof(MapID), (int) CurrentPlayer.MapTerritory))
                {
                Thread.Sleep(250);   
                continue;
                }

                SearchNearbyMobs();

                //1000/144 = 6.94 - so this should refresh a bit more than 144 times per sec, so animation should be 144+fps, right? seems legit. def smoother. cpu usage low.
                Thread.Sleep(_refreshRate);

            }
        }

        public void SearchNearbyMobs()
        {
            var tempNearbyList = new ObservableCollection<Mob>();

            var map = CurrentPlayer.MapTerritory;
            var actors = GetMobs();

            if (actors == null)
            {
                return;
            }

            var expansionDict = map <= 180 ? ARRDict : map <= 402 ? HWDict : map <= 622 ? SBDict : map <= 818 ? ShBDict : EWDict;

            foreach (var actor in actors)
            {
                //test check 
                if (actor.Value.HPMax > 100000)
                {
                    var m = actor.Value;
                    Console.WriteLine($"---------------------------------------------------------\n" +
                                      $"|{m.Name}| - ({ConvertPos(m.X)}, {ConvertPos(m.Y)}) - HP: {m.HPMax} , MAP TERRITORY = {m.MapTerritory}" +
                                      $"\n---------------------------------------------------------\n");
                }
                //END TEST PRINT

                var tempList = expansionDict.Values.FirstOrDefault(l =>
                    (l.FirstOrDefault(m =>
                    m.Name.ToLower() == actor.Value.Name.ToLower()
                        && (uint)m.MapTerritory == actor.Value.MapTerritory) != null));

                if (tempList == null)
                {
                    Console.WriteLine($"Null List - {expansionDict[HuntRank.S][0]}");
                }

                if (tempList != null)
                {
                    /*var m = actor.Value;
                    //Print
                    Console.WriteLine($"---------------------------------------------------------\n" +
                                      $"|{m.Name}| - ({ConvertPos(m.X)}, {ConvertPos(m.Y)}) - HP: {m.HPMax} , MAP TERRITORY = {m.MapTerritory}" +
                                      $"\n---------------------------------------------------------\n");*/

                    var mob = new Mob
                    {
                        Name = actor.Value.Name,
                        ModelID = (int)actor.Value.ModelID,
                        MapTerritory = (MapID)actor.Value.MapTerritory,
                        //Rank = tempList.FirstOrDefault(m => m.ModelID == actor.Value.ModelID)?.Rank,
                        Rank = tempList.FirstOrDefault(m => m.Name.ToLower() == actor.Value.Name.ToLower())?.Rank,
                        Coordinates = new Coords(ConvertPos(actor.Value.X), ConvertPos(actor.Value.Y)),
                        //MapImagePath = tempList.FirstOrDefault(m => m.ModelID == actor.Value.ModelID)?.MapImagePath ?? "no image path",
                        MapImagePath = tempList.FirstOrDefault(m => m.Name.ToLower() == actor.Value.Name.ToLower())?.MapImagePath ?? "no image path",
                        HP = actor.Value.HPMax,
                        X = actor.Value.X,
                        Y = actor.Value.Y,
                        Coordinate = actor.Value.Coordinate,
                        HPPercent = actor.Value.HPPercent,
                    };

                    tempNearbyList.Add(mob);

                    //if recording data for a train
                    if (CurrentTrainName != null)
                    {
                        if (Trains[CurrentTrainName].FirstOrDefault(m => m.ModelID == mob.ModelID) == null)
                        {
                            Trains[CurrentTrainName].Add(mob);
                        }
                    }
                }
            }

            #region modelID-Broken

            //bsaed on modelID - broken

            /*    var toRemove = new List<int>();
                //remove any old mobs that don't exist in the new list
                foreach (var mob in new Collection<Mob>(CurrentNearbyMobs))
                {
                    if (tempNearbyList.FirstOrDefault(m => m.ModelID == mob.ModelID) == null)
                    {
                        toRemove.Add(mob.ModelID);
                    }
                }

                toRemove.ForEach(i =>
                {
                    var mob = CurrentNearbyMobs.FirstOrDefault(m => m.ModelID == i);
                    mob.UnregisterHandlers(); // remove handlers from event
                    CurrentNearbyMobs.Remove(mob);
                });

                //add new mobs, update oldmobs coords.
                foreach (var tMob in tempNearbyList)
                {
                    if (CurrentNearbyMobs.FirstOrDefault(m => m.ModelID == tMob.ModelID) == null)
                    {
                        CurrentNearbyMobs.Add(tMob);
                    }
                    else
                    {
                        var mob = CurrentNearbyMobs.FirstOrDefault(m => m.ModelID == tMob.ModelID);
                        mob.Coordinates = tMob.Coordinates;
                        mob.HPPercent = tMob.HPPercent;
                    }
                }*/
            #endregion

            //based on Name
            var toRemoveName = new List<string>();
            //remove any old mobs that don't exist in the new list
            foreach (var mob in new Collection<Mob>(CurrentNearbyMobs))
            {
                if (tempNearbyList.FirstOrDefault(m => m.Name.ToLower() == mob.Name.ToLower()) == null)
                {
                    toRemoveName.Add(mob.Name);
                }
            }

            toRemoveName.ForEach(i =>
            {
                var mob = CurrentNearbyMobs.FirstOrDefault(m => m.Name.ToLower() == i.ToLower());
                mob.UnregisterHandlers(); // remove handlers from event
                CurrentNearbyMobs.Remove(mob);
            });

            //add new mobs, update old mobs coords.
            foreach (var tMob in tempNearbyList)
            {
                if (CurrentNearbyMobs.FirstOrDefault(m => m.Name.ToLower() == tMob.Name.ToLower()) == null)
                {
                    CurrentNearbyMobs.Add(tMob);
                }
                else
                {
                    var mob = CurrentNearbyMobs.FirstOrDefault(m => m.Name.ToLower() == tMob.Name.ToLower());

                    //if (mob.Coordinates.X != tMob.Coordinates.X || mob.Coordinates.Y != tMob.Coordinates.Y)

                    mob.Coordinates = tMob.Coordinates;


                    //if (mob.HPPercent != tMob.HPPercent)

                    mob.HPPercent = tMob.HPPercent;

                }
            }

            //Print info
            if (CurrentNearbyMobs.Count > 0)
            {
                Console.WriteLine($"CurrentNearbyMobs: {CurrentNearbyMobs.Count}");
                //Trace.WriteLine($"CurrentNearbyMobs: {CurrentNearbyMobs.Count}");
            }
        }

        #region  Stuff

        public void GetUser()
        {
            var user = _memoryReader.GetUser();

            if (user.MapTerritory < 813)
            {
                _ssMap = false;
            }

            CurrentPlayer.Coordinates = new Coords(ConvertPos(user.X), ConvertPos(user.Y));
            CurrentPlayer.Name = user.Name;
            CurrentPlayer.MapTerritory = user.MapTerritory;
            CurrentPlayer.SetMapImagePath(_ssMap);
            CurrentPlayer.PlayerIconImagePath = Globals.PlayerIconImagePath;
            CurrentPlayer.Heading = user.Heading;
            // Console.WriteLine($"Heading: {CurrentPlayer.Heading}");
            CurrentPlayer.HPPercent = user.HPPercent;

        }

        public ConcurrentDictionary<uint, ActorItem> GetMobs()
        {
            return _memoryReader.GetMobs();
        }

        private void GetNearbyMobs()
        {
            //if using list
            /*CurrentNearbyMobs.ForEach(m =>
            {
                Console.WriteLine($"GetNearbyMobs : {m.Name} - {m.Coordinates}: {Sharlayan.Utilities.ZoneLookup.GetZoneInfo((uint)m.MapTerritory).Name.English} ");
            });*/

            foreach (var m in CurrentNearbyMobs)
            {
                Console.WriteLine($"GetNearbyMobs : {m.Name} - {m.Coordinates}: {Sharlayan.Utilities.ZoneLookup.GetZoneInfo((uint)m.MapTerritory).Name.English}");
            }
        }

        #endregion

        #region Helpers / Event Handlers
        private double ConvertPos(double num) //works for ShB
        {
            //this converts into  x y pos on most (open world? combat-enabled?) maps, doesn't work for city states, residential areas, gold saucer etc
            //basically, if the map goes up to (41.9, 41.9) this will work. 
            //idk 21.48 seems more accurate than 21.5. can't remember how I got this initially... something to do with 42 coords on map, + 0.5 because ?
            //but actually shown as to 41.9, in-game excludes second decimal - so actually 41.96ish? idk idk idk

            //Trace.WriteLine(((Math.Floor((21.48 + (Convert.ToDouble(num) / 50)) * 100)) / 100));

            return ((Math.Floor((21.48 + (Convert.ToDouble(num) / 50)) * 100)) / 100);
        }


        private void LogSRanks(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_logS)
            {
                if (e.NewItems?.Count > 0)
                {
                    var text = "";

                    foreach (Mob mob in e.NewItems.Cast<Mob>().Where(m => m.Rank == "S" || m.Rank == "SS"))
                    {
                        text += ($"{mob.Name}\t {mob.Coordinates}\t Local: {DateTime.Now} | UTC: {DateTime.UtcNow}{Environment.NewLine}");
                    }
                    var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/UFHT S Log.txt";

                    File.AppendAllText(path, text);
                }
            }
        }

        private void SRankTTS(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_SRankTTS)
            {
                if (e.NewItems?.Count > 0)
                {
                    var text = "";

                    foreach (Mob mob in e.NewItems.Cast<Mob>().Where(m => m.Rank == "S" || m.Rank == "SS"))
                    {
                        _ = _tts.SpeakAsync($"{mob.Name} in zone.");
                    }
                }
            }
        }

        private void ARankTTS(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ARankTTS)
            {
                if (e.NewItems?.Count > 0)
                {
                    var text = "";

                    foreach (Mob mob in e.NewItems.Cast<Mob>().Where(m => m.Rank == "A"))
                    {
                        _ = _tts.SpeakAsync($"{mob.Name} nearby.");
                    }
                }
            }
        }
        private void BRankTTS(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_BRankTTS)
            {
                if (e.NewItems?.Count > 0)
                {
                    var text = "";

                    foreach (Mob mob in e.NewItems.Cast<Mob>().Where(m => m.Rank == "B"))
                    {
                        _ = _tts.SpeakAsync($"{mob.Name} nearby.");
                    }
                }
            }
        }

        public void SetRefreshRate(int rate)
        {
            //1000 divided by refresh rate, since it's milliseconds per refresh
            if (rate < 10 || rate > 300)
            {
                _refreshRate = 1000 / 60;
            }
            else _refreshRate = 1000 / rate;


        }

        #endregion

        #region bool toggles

        public void ToggleSSMap()
        {
            _ssMap = !_ssMap;
        }

        public void ToggleLogS(bool logS)
        {
            _logS = logS;
        }

        public void ToggleSRankTTS(bool sRankTTS)
        {
            _SRankTTS = sRankTTS;
        }

        public void ToggleARankTTS(bool aRankTTS)
        {
            _ARankTTS = aRankTTS;
        }

        public void ToggleBRankTTS(bool bRankTTS)
        {
            _BRankTTS = bRankTTS;
        }

        #endregion

    }
}
