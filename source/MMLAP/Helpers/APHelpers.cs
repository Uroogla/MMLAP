using Archipelago.Core;
using Archipelago.Core.Models;
using MMLAP.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Linq;
using static MMLAP.Models.Enums;

namespace MMLAP.Helpers
{
    public class APHelpers
    {
        public static async void OnConnected(object sender, EventArgs args, ArchipelagoClient client)
        {
            Log.Logger.Information("Connected to Archipelago");
            Log.Logger.Information($"Playing {client.CurrentSession.ConnectionInfo.Game} as {client.CurrentSession.Players.GetPlayerName(client.CurrentSession.ConnectionInfo.Slot)}");

        }

        //public static void OnDisconnected(object sender, EventArgs args)
        public static async void OnDisconnected()
        {
            Log.Logger.Information("Disconnected from Archipelago");
        }

        public static async void ItemReceived(object sender, ItemReceivedEventArgs args, ArchipelagoClient client)
        {
            if (client.CurrentSession == null) return;
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

        public static async void Client_MessageReceived(object sender, MessageReceivedEventArgs e){
            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
        }

        public static async void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
        {
            
        }
    }
}
