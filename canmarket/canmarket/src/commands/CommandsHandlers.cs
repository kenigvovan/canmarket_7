using canmarket.src.BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace canmarket.src.commands
{
    public static class CommandsHandlers
    {
        public static void RegisterServerCommands(ICoreServerAPI api)
        {
            api.ChatCommands.GetOrCreate("canmarket").HandleWith(CommandsHandlers.canHandlerCommand)
               .RequiresPlayer().RequiresPrivilege(Privilege.controlserver).IgnoreAdditionalArgs();
        }
        public static TextCommandResult canHandlerCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                return tcr;
            }
            if (args.RawArgs.Length < 2)
            {
                return tcr;
            }
            if (args.RawArgs[0].Equals("cn"))
            {
                var sel = player.Entity.BlockSelection;
                var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
                if (be is BECANMarket)
                {
                    (be as BECANMarket).ownerName = args.RawArgs[1];
                    be.MarkDirty();
                }
                else if (be is BECANStall)
                {
                    (be as BECANStall).ownerName = args.RawArgs[1];
                    foreach (var pl in player.Entity.Api.World.AllOnlinePlayers)
                    {
                        if (pl.PlayerName.Equals(args.RawArgs[1]))
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
                    (be as BECANMarketSingle).ownerName = args.RawArgs[1];
                    be.MarkDirty();
                }
            }
            else if (args.RawArgs[0].Equals("si") && args.RawArgs.Length > 1)
            {
                var sel = player.Entity.BlockSelection;
                var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
                if (be is BECANMarket)
                {
                    (be as BECANMarket).InfiniteStocks = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }
                else if (be is BECANStall)
                {
                    (be as BECANStall).InfiniteStocks = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }

            }
            else if (args.RawArgs[0].Equals("sp") && args.RawArgs.Length > 1)
            {
                var sel = player.Entity.BlockSelection;
                var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
                if (be is BECANMarket)
                {
                    (be as BECANMarket).StorePayment = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }
                else if (be is BECANStall)
                {
                    (be as BECANStall).StorePayment = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }
            }
            else if (args.RawArgs[0].Equals("as") && args.RawArgs.Length > 1)
            {
                var sel = player.Entity.BlockSelection;
                var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
                if (be is BECANStall)
                {
                    (be as BECANStall).adminShop = args.RawArgs[1].Equals("on");
                    (be as BECANStall).ownerUID = "";
                    be.MarkDirty();
                }
            }
            return tcr;
        }
    }
}
