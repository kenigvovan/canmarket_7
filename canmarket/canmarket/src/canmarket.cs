using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Cairo;
using canmarket.src.BE;
using canmarket.src.BEB;
using canmarket.src.Blocks;
using canmarket.src.commands;
using canmarket.src.Items;
using canmarket.src.Utils;
using HarmonyLib;
using ImGuiNET;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using VSImGui;
using VSImGui.API;

namespace canmarket.src
{
    public class canmarket : ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "canmarket.Patches";
        public static Config config;
        public ICoreClientAPI capi;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public static LoadedTexture myTex = null;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("itemcangear", typeof(ItemCANGearPayment));
            api.RegisterItemClass("itemcanchestslist", typeof(ItemCANStallBook));

            api.RegisterBlockClass("BlockCANMarket", typeof(BlockCANMarket));
            api.RegisterBlockClass("BlockCANMarketSingle", typeof(BlockCANMarketSingle));
            api.RegisterBlockClass("BlockCANMarketStall", typeof(BlockCANMarketStall));

            api.RegisterBlockClass("BlockCANWareHouse", typeof(BlockCANWareHouse));
            api.RegisterBlockEntityClass("BECANMarket", typeof(BECANMarket));
            api.RegisterBlockEntityClass("BECANMarketSingle", typeof(BECANMarketSingle));
            api.RegisterBlockEntityClass("BECANMarketStall", typeof(BECANMarketStall));
            api.RegisterBlockClass("BlockCANStall", typeof(BlockCANStall));
            api.RegisterBlockEntityClass("BECANStall", typeof(BECANStall));
            api.RegisterBlockEntityClass("BECANWareHouse", typeof(BECANWareHouse));
            api.RegisterBlockEntityBehaviorClass("marketlast", typeof(BEBehaviorTrackLastUpdatedContainer));
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            LoadConfig(api);
            config.IGNORED_STACK_ATTRIBTES_ARRAY = GlobalConstants.IgnoredStackAttributes.Concat(canmarket.config.IGNORED_STACK_ATTRIBTES_LIST.ToArray()).ToArray();
            api.Event.TestBlockAccess += (IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response) =>
            {
                if (accessType == EnumBlockAccessFlags.Use && blockSel.Block != null && (blockSel.Block is BlockCANMarket || blockSel.Block is BlockCANStall || blockSel.Block is BlockCANMarketSingle || blockSel.Block is BlockCANMarketStall))
                {
                    claimant = "";
                    return EnumWorldAccessResponse.Granted;
                }
                return response;
            };
            clientChannel = api.Network.RegisterChannel("canmarket");
            clientChannel.RegisterMessageType(typeof(SyncConfigPacket));
            clientChannel.SetMessageHandler<SyncConfigPacket>((packet) =>
            {
                Config deserialized;
                using (var ms = new MemoryStream(packet.data))
                {
                    deserialized = Serializer.Deserialize<Config>(ms);
                }
                config.SEARCH_WAREHOUE_DISTANCE = deserialized.SEARCH_WAREHOUE_DISTANCE;
                config.SEARCH_CONTAINER_RADIUS = deserialized.SEARCH_CONTAINER_RADIUS;
                config.PERISH_DIVIDER = deserialized.PERISH_DIVIDER;
            });
            //api.ModLoader.GetModSystem<ImGuiModSystem>().Draw += Draw;
        }
        /*private CallbackGUIStatus OnDraw(float deltaSeconds)
        {
            if (!showWindow) return CallbackGUIStatus.Closed;

            // Ustaw rozmiar okna przy pierwszym użyciu
            ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.FirstUseEver);

            // Wycentruj okno przy pierwszym użyciu
            var displaySize = ImGui.GetIO().DisplaySize;
            var windowSize = new Vector2(500, 300);
            var windowPos = new Vector2(
                (displaySize.X - windowSize.X) * 0.5f,
                (displaySize.Y - windowSize.Y) * 0.5f
            );
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.FirstUseEver);

            if (ImGui.Begin(translation.Get("temperature_history"), ref showWindow))
            {
                DrawTemperatureData();
            }
            ImGui.End();

            return showWindow ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
        }*/
        /*private CallbackGUIStatus Draw(float deltaSeconds)
        {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                 | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            ImGuiWindowFlags flags1 = 
                 ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground;
            //ImGui.Begin("effectBox", flags);
            ImGui.Begin("ImGui example", flags1);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.9f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.3f, 0.7f, 1.0f));
            if (ImGui.Button("fffff1232", new Vector2(750, 40)))
            {
                // Действие при нажатии
            }

            ImGui.PopStyleColor(3);
            

            ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.FirstUseEver);
            var displaySize = ImGui.GetIO().DisplaySize;
            var windowSize = new Vector2(500, 300);
            var windowPos = new Vector2(
                (displaySize.X - windowSize.X) * 0.5f,
                (displaySize.Y - windowSize.Y) * 0.5f
            );
            float roll = capi.World.Player.CameraRoll * GameMath.RAD2DEG;
            ImGui.SliderFloat("Roll", ref roll, -90, 90);
           
            if (canmarket.myTex == null)
            {
                ImageSurface surface = new ImageSurface(0, 1, 1);
                Context context = new Context(surface);
                context.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
                context.Paint();
                canmarket.myTex = new LoadedTexture(this.capi);
                capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref myTex);
                context.Dispose();
                surface.Dispose();
            }
            var c = ImGui.GetBackgroundDrawList();
            c.AddImage(myTex.TextureId, new(150), new(600));
            //ImGui.Image(tt.TextureId,new(200));
            capi.World.Player.CameraRoll = roll * GameMath.DEG2RAD;
            if (ImGui.ImageButton("", myTex.TextureId, new Vector2(40)))
            {
                // действие
            }
            ImGui.PopFont();
            var f2 = ImGui.GetIO().Fonts.Fonts[6];
            ImGui.PushFont(f2);
            var p = ImGui.GetFont();
            //ImGui.PushFont
            ImGui.RadioButton("hello", false);
            byte[] f = new byte[16];
            ImGui.InputText("here", f, 16);
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.ShowUserGuide();
            ImGui.End();

            return CallbackGUIStatus.GrabMouse;
        }*/
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            CommandsHandlers.RegisterServerCommands(api);
            LoadConfig(api);
            api.Event.OnTestBlockAccess += TestBlockAccessDelegateServer;

            config.IGNORED_STACK_ATTRIBTES_ARRAY = GlobalConstants.IgnoredStackAttributes.Concat(canmarket.config.IGNORED_STACK_ATTRIBTES_LIST.ToArray()).ToArray();
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.InventoryBase).GetMethod("DidModifyItemSlot"), postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_InventoryBase_OnItemSlotModified")));
            serverChannel = api.Network.RegisterChannel("canmarket");

            api.Event.PlayerNowPlaying += (IServerPlayer byPlayer) =>
            {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, config);
                    data = ms.ToArray();
                }
                serverChannel.SendPacket(new SyncConfigPacket() { data = data }, byPlayer);
            };
            
            serverChannel.RegisterMessageType(typeof(SyncConfigPacket));
        }
        public EnumWorldAccessResponse TestBlockAccessDelegateServer(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response)
        {
            if(blockSel == null || accessType != EnumBlockAccessFlags.Use || (player?.Entity == null))
            {
                return response;
            }
            var bl = player.Entity.Api?.World.BlockAccessor?.GetBlock(blockSel.Position) ?? null;
            if (accessType == EnumBlockAccessFlags.Use && bl != null && (bl is BlockCANMarket || bl is BlockCANStall || bl is BlockCANMarketSingle || bl is BlockCANMarketStall))
            {
                claimant = "";
                return EnumWorldAccessResponse.Granted;
            }
            return response;
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
            harmonyInstance?.UnpatchAll(harmonyID);        
            harmonyInstance = null;
            config = null;
        }
    }
}
