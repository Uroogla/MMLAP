using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
using Archipelago.Core.Helpers;
using Archipelago.Core.Models;
using Archipelago.Core.Util;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using MMLAP.Helpers;
using MMLAP.Models;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Timers;
using static MMLAP.Models.Enums;

namespace MMLAP;

public partial class App : Application
{
    // TODO: Remember to set this in MMLAP.Desktop as well.
    public static string Version = "0.0.1";
    public static List<string> SupportedVersions = ["0.0.1"];

    public static MainWindowViewModel Context;
    public static ArchipelagoClient APClient { get; set; }
    public static List<ILocation> GameLocations { get; set; }
    public static Dictionary<long, ItemData> scoutedLocationItemData { get; set; }
    private static string _playerName { get; set; }
    private static bool _hasSubmittedGoal { get; set; }
    private static Timer _gameLoopTimer { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static bool IsRunningAsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Start();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Context
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = Context
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void Start()
    {
        Context = new MainWindowViewModel("0.6.3 or later");
        Context.ClientVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

        Context.ConnectClicked += Context_ConnectClicked;
        Context.CommandReceived += Context_CommandReceived;
        Context.OverlayEnabled = true;
        Context.AutoscrollEnabled = true;
        Context.ConnectButtonEnabled = true;

        _hasSubmittedGoal = false;

        Log.Logger.Information("This Archipelago Client is compatible only with the NTSC-U release of Mega Man Legends.");
        Log.Logger.Information("Trying to play with a different version will not work as intended.");
        if (!IsRunningAsAdministrator())
        {
            Log.Logger.Warning("You do not appear to be running this client as an administrator.");
            Log.Logger.Warning("This may result in errors or crashes when trying to connect to Duckstation.");
        }
    }

    private void Context_CommandReceived(object? sender, ArchipelagoCommandEventArgs a)
    {
        if (string.IsNullOrWhiteSpace(a.Command)) return;
        APClient.SendMessage(a.Command);
        string command = a.Command.Trim().ToLower();
        switch (command)
        {
            case "reload":
                Log.Logger.Information("Clearing the game state.  Please reconnect to the server while in game to refresh received items.");
                APClient.ItemManager.ForceReloadAllItems();
                break;
            default:
                Log.Logger.Information("Command not recognized.");
                break;
        }
    }

    private void HandleCommand(string command)
    {
        if (APClient == null || APClient.ItemManager == null || APClient.CurrentSession == null) return;
        switch (command)
        {
            case "showGoal":
                CompletionGoal goal = (CompletionGoal)int.Parse(APClient.Options?.GetValueOrDefault("goal", 0).ToString());
                string goalText;
                switch (goal)
                {
                    case CompletionGoal.Juno:
                        goalText = "Defeat Juno.";
                        break;
                    default:
                        goalText = "Unknown.";
                        break;
                }
                Log.Logger.Information($"Your goal is: {goalText}");
                break;
        }
    }

    private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
    {
        if (APClient != null)
        {
            APClient.CancelMonitors();
            APClient.Connected -= OnConnected;
            APClient.Disconnected -= OnDisconnected;
            APClient.ItemReceived -= ItemReceived;
            APClient.MessageReceived -= Client_MessageReceived;
            APClient.LocationCompleted -= Client_LocationCompleted;
            APClient.CurrentSession.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
            _unlockedLevels = 0;
        }
        GameClient? gameClient = null;
        try
        {
            gameClient = new GameClient("duckstation");
        }
        catch (ArgumentException ex)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        bool DuckstationConnected = gameClient.Connect();
        if (!DuckstationConnected)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        APClient = new ArchipelagoClient(gameClient);
        Memory.GlobalOffset = Memory.GetDuckstationOffset();

        APClient.Connected += OnConnected;
        APClient.Disconnected += OnDisconnected;
        APClient.LocationCompleted += Client_LocationCompleted;
        APClient.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        APClient.MessageReceived += Client_MessageReceived;
        APClient.ItemReceived += ItemReceived;
        APClient.EnableLocationsCondition = () => Helpers.IsInGame();

        await APClient.Connect(e.Host, "Mega Man Legends", "save1");
        if (!APClient.IsConnected)
        {
            Log.Logger.Error("Your host seems to be invalid.  Please confirm that you have entered it correctly.");
            return;
        }

        _playerName = e.Slot;
        await APClient.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
        if (Client.Options?.Count > 0)
        {
            // GemsanityOptions gemsanityOption = (GemsanityOptions)int.Parse(Client.Options?.GetValueOrDefault("enable_gemsanity", "0").ToString());
            int slot = Client.CurrentSession.ConnectionInfo.Slot;
            Dictionary<string, object> slotData = await Client.CurrentSession.DataStorage.GetSlotDataAsync(slot);
            List<int> gemsanityIDs = new List<int>();
            if (slotData.TryGetValue("gemsanity_ids", out var value))
            {
                if (value != null)
                {
                    gemsanityIDs = System.Text.Json.JsonSerializer.Deserialize<List<int>>(value.ToString());
                }
            }
            if (slotData.TryGetValue("apworldVersion", out var versionValue))
            {
                if (versionValue != null && SupportedVersions.Contains(versionValue.ToString().ToLower()))
                {
                    Log.Logger.Information($"The host's AP world version is {versionValue.ToString()} and the client version is {Version}.");
                    Log.Logger.Information("These versions are known to be compatible.");
                }
                else if (versionValue != null && versionValue.ToString().ToLower() != Version.ToLower())
                {
                    Log.Logger.Warning($"The host's AP world version is {versionValue.ToString()} but the client version is {Version}.");
                    Log.Logger.Warning("Please ensure these are compatible before proceeding.");
                }
                else if (versionValue == null)
                {
                    Log.Logger.Error("This will almost certainly result in errors.");
                }
            }
            else
            {
                Log.Logger.Error("This will almost certainly result in errors.");
            }
            //_requiredOrbs = int.Parse(Client.Options?.GetValueOrDefault("ripto_door_orbs", 0).ToString());
            GameLocations = LocationHelpers.BuildLocationList();
            GameLocations = GameLocations.Where(x => x != null && !Client.CurrentSession.Locations.AllLocationsChecked.Contains(x.Id)).ToList();
            Client.MonitorLocations(GameLocations);

            Log.Logger.Information("Warnings and errors above are okay if this is your first time connecting to this multiworld server.");
        }
        else
        {
            Log.Logger.Error("Failed to login.  Please check your host, name, and password.");
        }
    }

    private void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
    {
        if (APClient.LocationManager == null || APClient.CurrentSession == null) return;

        // Use scouted location item to rewrite textbox

        // Do goal completions need to be included as locations?
        CheckGoalCondition();
    }

    private async void ItemReceived(object? sender, ItemReceivedEventArgs args)
    {
        if (APClient.CurrentSession == null) return;
        Log.Logger.Debug($"Item Received: {JsonConvert.SerializeObject(args.Item)}");
        switch (args.Item)
        {
            case Item x when Enum.TryParse<ItemCategory>(x.Category, out ItemCategory category) && (category == ItemCategory.Nothing):
                ItemHelpers.ReceiveNothing(x); break;
            case Item x when Enum.TryParse<ItemCategory>(x.Category, out ItemCategory category) && (category == ItemCategory.Zenny):
                ItemHelpers.ReceiveZenny(x); break;
            case Item x when Enum.TryParse<ItemCategory>(x.Category, out ItemCategory category) && (category == ItemCategory.Buster):
                ItemHelpers.ReceiveBusterPart(x); break;
            case Item x when Enum.TryParse<ItemCategory>(x.Category, out ItemCategory category) && (category == ItemCategory.Special):
                ItemHelpers.ReceiveSpecialItem(x); break;
            case Item x when Enum.TryParse<ItemCategory>(x.Category, out ItemCategory category) && (category == ItemCategory.Normal):
                ItemHelpers.ReceiveNormalItem(x); break;
            default:
                Console.WriteLine($"Item not recognised. ({args.Item.Name}) Skipping"); break;
        };
    }

    private static async void ModifyGameLoop(object? sender, ElapsedEventArgs e)
    {
        if (!Helpers.IsInGame() || APClient.ItemManager == null || APClient.CurrentSession == null)
        {
            return;
        }
        try
        {
            // Avoid inadvertantly messing with the Atlas and Options overlays when loaded in on the pause screen.
            // This can result in corrupted save files.
            //GameStatus status = (GameStatus)Memory.ReadByte(Addresses.GetVersionAddress(Addresses.GameStatus));
            //if (status == GameStatus.Paused)
            //{
            //    return;
            //}
            CheckGoalCondition();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning("Encountered an error while managing the game loop.");
            Log.Logger.Warning(ex.ToString());
            Log.Logger.Warning("This is not necessarily a problem if it happens during release or collect.");
        }
    }

    private static void CheckGoalCondition()
    {
        if (
            APClient == null ||
            APClient.CurrentSession == null ||
            APClient.CurrentSession.Locations == null ||
            APClient.CurrentSession.Locations.AllLocationsChecked == null ||
            APClient.ItemManager == null ||
            GameLocations == null
            // check in game?
        )
        {
            return;
        }
        if (_hasSubmittedGoal)
        {
            return;
        }
        
        int goal = int.Parse(APClient.Options?.GetValueOrDefault("goal", 0).ToString());
        bool isGoalComplete = (CompletionGoal)goal switch
        {
            CompletionGoal.Juno => Memory.ReadBit(Addresses.GoalJuno.Address, Addresses.GoalJuno.BitNumber ?? 0),
            _ => false
        };
        if (isGoalComplete)
        {
            APClient.SendGoalCompletion();
            _hasSubmittedGoal = true;
        }
    }

    private static async void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
    }

    private static async void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
    {
        if (Client.ItemState == null || Client.CurrentSession == null) return;
        if (!Helpers.IsInGame())
        {
            Log.Logger.Error("Check sent while not in game. Please report this in the Discord thread!");
        }
        CheckGoalCondition();
    }

    private static async void OnConnected(object? sender, EventArgs args)
    {
        // Start gameplay loop
        _gameLoopTimer = new Timer();
        _gameLoopTimer.Elapsed += new ElapsedEventHandler(ModifyGameLoop);
        _gameLoopTimer.Interval = 500;
        _gameLoopTimer.Enabled = true;

        // Load locations and start monitoring
        List<ILocation> locations = LocationHelpers.BuildLocationList();
        await APClient.MonitorLocationsAsync(locations);

        // Scout locations for items
        long[] locationIds = (from loc in locations select (long)loc.Id).ToArray();
        ArchipelagoSession session = APClient.CurrentSession;
        Dictionary<long, ScoutedItemInfo> scoutedLocations = await session.Locations.ScoutLocationsAsync(locationIds);
        Dictionary<long, ItemData> itemDataDict = LocationHelpers.GetItemDataDict();
        scoutedLocationItemData = scoutedLocations.Keys.ToDictionary(locationId => locationId, locationId => itemDataDict[scoutedLocations[locationId].ItemId]);

        Log.Logger.Information("Connected to Archipelago");
        Log.Logger.Information($"Playing {APClient.CurrentSession.ConnectionInfo.Game} as {APClient.CurrentSession.Players.GetPlayerName(APClient.CurrentSession.ConnectionInfo.Slot)}");
    }

    private static async void OnDisconnected(object? sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
        // Avoid ongoing timers affecting a new game.
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        _unlockedLevels = 0;
        Log.Logger.Information("This Archipelago Client is compatible only with the USA release of Mega Man Legends.");
        Log.Logger.Information("Trying to play with a different version will not work as intended.");

        //if (_deathLinkService != null)
        //{
        //    _deathLinkService = null;
        //}
    }
}
