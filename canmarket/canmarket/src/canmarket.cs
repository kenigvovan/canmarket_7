using canmarket.src.BE;
using canmarket.src.BEB;
using canmarket.src.Blocks;
using canmarket.src.Items;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace canmarket.src
{
    public class canmarket : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canmods.Patches";
        public string[] ignoredStackAttribtes;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("itemcangear", typeof(ItemCANGearPayment));
            api.RegisterItemClass("itemcanchestslist", typeof(ItemCANStallBook));

            api.RegisterBlockClass("BlockCANMarket", typeof(BlockCANMarket));
            api.RegisterBlockClass("BlockCANWareHouse", typeof(BlockCANWareHouse));
            api.RegisterBlockEntityClass("BECANMarket", typeof(BECANMarket));
            api.RegisterBlockClass("BlockCANStall", typeof(BlockCANStall));
            api.RegisterBlockEntityClass("BECANStall", typeof(BECANStall));
            api.RegisterBlockEntityClass("BECANWareHouse", typeof(BECANWareHouse));
            api.RegisterBlockEntityBehaviorClass("marketlast", typeof(BEBehaviorTrackLastUpdatedContainer));
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            // api.Event.OnTestBlockAccess += TestBlockAccessDelegate;
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.Client.NoObf.ClientEventAPI).GetMethod("TriggerTestBlockAccess"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("TriggerTestBlockAccess_Patch")));
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("UpdateAndGetTransitionStatesNative",
                BindingFlags.NonPublic | BindingFlags.Instance), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_UpdateAndGetTransitionStatesNative")));

        }
        /*   public EnumWorldAccessResponse TestBlockAccessDelegate(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response)
           {
               if(accessType == EnumBlockAccessFlags.Use)
               {
                   if(blockSel.Block != null && blockSel.Block.Class.Equals)
                   {
                       return EnumWorldAccessResponse.Granted;
                   }
               }          
           }*/
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.ChatCommands.Create("canmarket").HandleWith(canHandlerCommand)
               .RequiresPlayer().RequiresPrivilege(Privilege.controlserver).IgnoreAdditionalArgs();
            loadConfig(api);
            List<string> tmpArr = new List<string>();
            foreach(var it in GlobalConstants.IgnoredStackAttributes)
            {
                tmpArr.Add(it);
            }
            foreach(var it in Config.Current.IGNORED_STACK_ATTRIBTES_LIST.Val.ToArray())
            {
                 tmpArr.Add(it);                
            }
            Config.Current.IGNORED_STACK_ATTRIBTES_ARRAY.Val = tmpArr.ToArray();
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.InventoryBase).GetMethod("DidModifyItemSlot"), postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_InventoryBase_OnItemSlotModified")));
            
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
                if(be is BECANMarket)
                {
                    (be as BECANMarket).ownerName = args.RawArgs[1];
                    be.MarkDirty();
                }
                else if (be is BECANStall)
                {
                    (be as BECANStall).ownerName = args.RawArgs[1];
                    foreach(var pl in player.Entity.Api.World.AllOnlinePlayers)
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
                    //be.MarkDirty();
                }
            }
            else if(args.RawArgs[0].Equals("si") && args.RawArgs.Length > 1)
            {
                var sel = player.Entity.BlockSelection;
                var be = player.Entity.Api.World.BlockAccessor.GetBlockEntity(sel.Position);
                if (be is BECANMarket)
                {
                    (be as BECANMarket).InfiniteStocks = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }
                else if(be is BECANStall)
                {
                    (be as BECANStall).InfiniteStocks = args.RawArgs[1].Equals("on");
                    be.MarkDirty();
                }
                
            }
            else if(args.RawArgs[0].Equals("sp") && args.RawArgs.Length > 1)
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
        private void loadConfig(ICoreAPI api)
        {
            try
            {
                Config.Current = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
                if (Config.Current != null)
                {
                    api.StoreModConfig<Config>(Config.Current, this.Mod.Info.ModID + ".json");
                    return;
                }
            }
            catch (Exception e)
            {

            }

            Config.Current = new Config();
            api.StoreModConfig<Config>(Config.Current, this.Mod.Info.ModID + ".json");
            return;
        }
        public override void Dispose()
        {
            base.Dispose();
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchAll(harmonyID);
            }
            harmonyInstance = null;
        }
    }
}
