using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using VSImGui;

namespace runestory
{
    public class runestoryModSystem : ModSystem
    {
        public static ILogger Runelogger;

        public static ICoreClientAPI runeApi;
        public ImGuiModSystem runeGuiSys;

        public SpellWindow theOneGui;

        public IClientNetworkChannel capi_Runechannel;
        public IServerNetworkChannel sapi_Runechannel;

        public List<BaseRuneSpell> AllSpells = [];
        public override void StartPre(ICoreAPI api)
        {
            if (api is not ICoreClientAPI capi) return;

            if (theOneGui == null)
            {
                theOneGui = new();
            }
            runeApi = capi;
            capi.Input.RegisterHotKey("runestorytogglespell", Lang.Get("runestory:toggle-spell"), GlKeys.F, HotkeyType.GUIOrOtherControls, true);
            capi.Input.RegisterHotKey("runestorycastspell", Lang.Get("runestory:cast-spell"), GlKeys.X, HotkeyType.CharacterControls);


            capi.Input.SetHotKeyHandler("runestorytogglespell", delegate { theOneGui.ToggleOpen(); return true; } );
            capi.Input.SetHotKeyHandler("runestorycastspell", delegate { OnCastRequest(capi); return true; });
        }

        public void OnCastRequest(ICoreClientAPI capi)
        {
            capi.Network.GetChannel("runespellchannel").SendPacket(new CTS_SpellPacket
            {
                byPlayerID = capi.World.Player.Entity.EntityId,
            });
        }
        private void OnRecCastRequest(IPlayer from,CTS_SpellPacket pack)
        {
            //Todo: spawn and register ents
            Runelogger.Log(EnumLogType.Debug,"Succ" + from.Entity.Attributes.GetString("runespellselected", "BAD"));
        }
        private void OnRecSpellSelect(IServerPlayer fromPlayer, CTS_SelectPacket pack)
        {
            fromPlayer.Entity.Attributes.SetString("runespellselected", pack.spellID);
        }
        public override void Start(ICoreAPI api)
        { 
            Runelogger = api.Logger;
            

            AllSpells = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BaseRuneSpell>>("runespells").Recipes;

            if(api.Side == EnumAppSide.Client)
            {
                capi_Runechannel = (api as ICoreClientAPI).Network.RegisterChannel("runespellchannel")
                    .RegisterMessageType(typeof(CTS_SpellPacket))
                    .RegisterMessageType(typeof(CTS_SelectPacket));
            }
            else
            {
                sapi_Runechannel = (api as ICoreServerAPI).Network.RegisterChannel("runespellchannel")
                    .RegisterMessageType(typeof(CTS_SpellPacket))
                    .SetMessageHandler<CTS_SpellPacket>(OnRecCastRequest)
                    .RegisterMessageType(typeof(CTS_SelectPacket))
                    .SetMessageHandler<CTS_SelectPacket>(OnRecSpellSelect);
            }

            api.Logger.Notification("RuneStory is coded by Len, god save us all.");
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Runestory is running. Why would you do this to your poor server.");
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if(api is not ICoreServerAPI sApi) { return; }
            bool classExclu = sApi.World.Config.GetBool("classExclusiveRecipes", true);


            LoadSpells(sApi);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            runeGuiSys = api.ModLoader.GetModSystem<ImGuiModSystem>();
            runeGuiSys.Draw += theOneGui.Draw;
            runeGuiSys.Closed += theOneGui.Close;

            api.Logger.Notification("Runestory? On my client? Its more likely than you think!");
        }

        public void LoadSpells(ICoreServerAPI api)
        {
            Dictionary<AssetLocation, JToken> loadedSpells = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/runespells");

            int loaded = 0;
            int failed = 0;
            int found = 0;
            foreach ((AssetLocation path, JToken jboi) in loadedSpells)
            {
                if (jboi is JObject yepjson)
                {

                    BaseRuneSpell? parsed = yepjson.ToObject<BaseRuneSpell>(path.Domain);

                    if (parsed == null)
                    {
                        api.Logger.Error($"Failed parsing spell at {path}");
                        continue;
                    }
                    LoadSpell<BaseRuneSpell>(api, path, parsed, ref loaded, ref failed);
                    found++;

                }
                else if (jboi is JArray jarray)
                {
                    foreach (JToken tok in jarray)
                    {
                        BaseRuneSpell? parsed = tok.ToObject<BaseRuneSpell>(path.Domain);

                        if (parsed == null)
                        {
                            api.Logger.Error($"Failed parsing spell at {path}");
                            continue;
                        }
                        LoadSpell<BaseRuneSpell>(api, path, parsed, ref loaded, ref failed);
                        found++;
                    }
                }
            }

            api.Logger.Log(EnumLogType.Debug, $"Loaded {loaded} spells.");
        }

        public void LoadSpell<T>(ICoreServerAPI api, AssetLocation assLoc, BaseRuneSpell spell, ref int loaded, ref int failed) where T : BaseRuneI<T>
        {
            if (!spell.Enabled) return;
            if (spell.Code == null) { spell.Code = assLoc; }

            AddSpell(spell);
            loaded++;
        }

        public void AddSpell(BaseRuneSpell boi)
        {
            AllSpells.Add(boi);
        }

        public override void Dispose()
        {
            theOneGui = null;
            base.Dispose();
        }
    }
}
