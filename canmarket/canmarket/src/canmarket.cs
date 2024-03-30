using canmarket.src.BE;
using canmarket.src.BEB;
using canmarket.src.Blocks;
using canmarket.src.commands;
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
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace canmarket.src
{
    public class canmarket : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canmarket.Patches";
        public static Config config;
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
            api.Event.TestBlockAccess += (IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response) =>
            {
                if(accessType == EnumBlockAccessFlags.Use && blockSel.Block != null && (blockSel.Block is BlockCANMarket || blockSel.Block is BlockCANStall))
                {
                    claimant = "";
                    return EnumWorldAccessResponse.Granted;
                }
                return response;
            };
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("UpdateAndGetTransitionStatesNative",
                BindingFlags.NonPublic | BindingFlags.Instance), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_UpdateAndGetTransitionStatesNative")));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            CommandsHandlers.RegisterServerCommands(api);
            LoadConfig(api);

            config.IGNORED_STACK_ATTRIBTES_ARRAY = GlobalConstants.IgnoredStackAttributes.Concat(canmarket.config.IGNORED_STACK_ATTRIBTES_LIST.ToArray()).ToArray();
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.InventoryBase).GetMethod("DidModifyItemSlot"), postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_InventoryBase_OnItemSlotModified")));      
        }  
        private void LoadConfig(ICoreAPI api)
        {
            //Try to read old config
            OldConfig oldConfig = null;
            try
            {
                oldConfig = api.LoadModConfig<OldConfig>(this.Mod.Info.ModID + ".json");
            }
            catch (Exception e)
            {

            }
            //old config was found and we just copy values from it
            if (oldConfig != null)
            {
                config = new Config();
                oldConfig.TranslateConfig(config);
                //make copy of the old config and new to old file
                try
                {
                    api.StoreModConfig<OldConfig>(oldConfig, this.Mod.Info.ModID + "_old.json");
                    api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
                }
                catch (Exception e)
                {

                }
                return;
            }
            //no old config, try to load new format
            else
            {
                //config = new Config();
                config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
               
                if (config == null)
                {
                    config = new Config();
                    api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
                    return;
                }
                api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
                return;
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchAll(harmonyID);
            }
            harmonyInstance = null;
            config = null;
        }
    }
}
