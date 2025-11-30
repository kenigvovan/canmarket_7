using System.Linq;
using canmarket.src.BE;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Server;

namespace canmarket.src.commands
{
    public static class CommandsHandlers
    {
        public static void RegisterServerCommands(ICoreServerAPI api)
        {
            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.GetOrCreate("canmarket").RequiresPlayer().RequiresPrivilege(Privilege.controlserver)
                       .BeginSub("cn").WithAlias("changename")
                            .HandleWith(CommandsHandlers.changeName)
                            .WithArgs(parsers.Word("newOwnerName"))
                            .WithDesc("Change owner")
                       .EndSub()
                       .BeginSub("si").WithAlias("setinfinite")
                            .HandleWith(CommandsHandlers.setInfiniteStocks)
                            .WithArgs(parsers.WordRange("state", "on", "off"))
                            .WithDesc("Set infinite stocks state")
                       .EndSub()
                       .BeginSub("sp").WithAlias("storepayment")
                            .HandleWith(CommandsHandlers.setStorePayment)
                            .WithArgs(parsers.WordRange("state", "on", "off"))
                            .WithDesc("Set store payment state")
                       .EndSub()
                       .BeginSub("as").WithAlias("adminshop")
                            .HandleWith(CommandsHandlers.setAdminShop)
                            .WithArgs(parsers.WordRange("state", "on", "off"))
                            .WithDesc("Set store as admin shop")
                       .EndSub();
        }
        public static TextCommandResult changeName(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                return tcr;
            }
            var sel = player.Entity.BlockSelection;
            var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be is BECANMarket)
            {
                (be as BECANMarket).ownerName = args.Parsers[0].GetValue().ToString();
                be.MarkDirty();
            }
            else if (be is BECANStall)
            {
                (be as BECANStall).ownerName = args.Parsers[0].GetValue().ToString();
                foreach (var pl in player.Entity.Api.World.AllOnlinePlayers)
                {
                    if (pl.PlayerName.Equals(args.Parsers[0].GetValue().ToString()))
                    {
                        (be as BECANStall).ownerUID = pl.PlayerUID;
                        be.MarkDirty();
                        return tcr;
                    }
                }
                (be as BECANStall).ownerUID = "1234";
                be.MarkDirty();
            }
            else if (be is BECANMarketSingle)
            {
                (be as BECANMarketSingle).ownerName = args.Parsers[0].GetValue().ToString();
                be.MarkDirty();
            }
            else if (be is BECANMarketStall)
            {
                var foundPlayer = be.Api.World.AllOnlinePlayers.FirstOrDefault(pl => pl.PlayerName.Equals(args.Parsers[0].GetValue().ToString()), null);
                if (foundPlayer != null)
                {
                    (be as BECANMarketStall).ownerName = foundPlayer.PlayerName;
                    (be as BECANMarketStall).ownerUID = foundPlayer.PlayerUID;
                }
                else
                {
                    (be as BECANMarketStall).ownerName = args.Parsers[0].GetValue().ToString();
                    (be as BECANMarketStall).ownerUID = args.Parsers[0].GetValue().ToString();
                }
                be.MarkDirty();
            }
            return tcr;
        }
        public static TextCommandResult setInfiniteStocks(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                return tcr;
            }
            var sel = player.Entity.BlockSelection;
            var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be is BECANMarket)
            {
                (be as BECANMarket).InfiniteStocks = args.Parsers[0].GetValue().ToString().Equals("on");
                be.MarkDirty();
            }
            else if (be is BECANStall)
            {
                (be as BECANStall).InfiniteStocks = args.Parsers[0].GetValue().ToString().Equals("on");
                be.MarkDirty();
            }
            return tcr;
        }
        public static TextCommandResult setStorePayment(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                return tcr;
            }
            var sel = player.Entity.BlockSelection;
            var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be is BECANMarket)
            {
                (be as BECANMarket).StorePayment = args.Parsers[0].GetValue().ToString().Equals("on");
                be.MarkDirty();
            }
            else if (be is BECANStall)
            {
                (be as BECANStall).StorePayment = args.Parsers[0].GetValue().ToString().Equals("on");
                be.MarkDirty();
            }
            return tcr;
        }
        public static TextCommandResult setAdminShop(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            var sel = player.Entity.BlockSelection;
            var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be is BECANStall)
            {
                (be as BECANStall).adminShop = args.Parsers[0].GetValue().ToString().Equals("on");
                (be as BECANStall).ownerUID = "admin";
                (be as BECANStall).ownerName = "";
                be.MarkDirty();
            }
            else if (be is BECANMarketStall)
            {
                (be as BECANMarketStall).adminShop = args.Parsers[0].GetValue().ToString().Equals("on");
                (be as BECANMarketStall).ownerUID = "admin";
                (be as BECANMarketStall).ownerName = "";
                be.MarkDirty();
            }
            return tcr;
        }
    }    
}
