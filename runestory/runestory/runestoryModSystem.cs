using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json.Linq;
using runestory.src.entity.spells;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using VSImGui;

namespace runestory
{
    public class runestoryModSystem : ModSystem
    {
        public static ILogger Runelogger;

        public static ICoreClientAPI runeCApi;
        public static ICoreServerAPI runeSApi;
        public ImGuiModSystem runeGuiSys;

        public SpellWindow theOneGui;

        public IClientNetworkChannel capi_Runechannel;
        public IServerNetworkChannel sapi_Runechannel;

        public List<BaseRuneSpell> AllSpells = [];
        public List<BaseRuneAltar> AltarRecipes = [];

        public override void StartPre(ICoreAPI api)
        {
            if (api is not ICoreClientAPI capi) return;

            if (theOneGui == null)
            {
                theOneGui = new();
            }
            runeCApi = capi;
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
            string spell = from.Entity.Attributes.GetString("runespellselected", "ERROR");
            long timenext = from.Entity.Attributes.GetLong("runespellnextcasttime");
            if (timenext > runeSApi.World.ElapsedMilliseconds)
            {
                ((runeSApi.World.PlayerByUid(from.Entity.PlayerUID))as IServerPlayer).SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "runestory:cast-fail-toosoon");
                return;
            }
            if ( spell != "ERROR") {
                defaultSpell boi = runeSApi.World.ClassRegistry.CreateEntity(runeSApi.World.GetEntityType(new("runestory:basestartrunespell"))) as defaultSpell;
                boi.spawnedBy = from.Entity;
                boi.spellCode = spell;
                Vec3d pos = from.Entity.Pos.XYZ.AddCopy(0, from.Entity.LocalEyePos.Y, 0);
                boi.Pos.SetPos(from.Entity.Pos.Copy().XYZ.Add(0, from.Entity.LocalEyePos.Y, 0));
                boi.World = from.Entity.World;
                runeSApi.World.SpawnPriorityEntity(boi);
                SetCastDelay(from.Entity, AllSpells.Find(thing => thing.Code == spell));
            }
        }
        public void SetCastDelay(Entity ent, BaseRuneSpell spell)
        {
            long percastMS = 500;
            int TotalReag = 0;
            if (spell is not null)
            {
                for (int i = 0; i < spell.Reagents.Count; i++)
                {
                    TotalReag += spell.Reagents.ElementAt(i).Value;
                }
            }
            long toset = (percastMS * (TotalReag == 0 ? 1 : TotalReag)) + runeSApi.World.ElapsedMilliseconds;
            ent.Attributes.SetLong("runespellnextcasttime", toset);
            ent.Attributes.MarkPathDirty("runespellnextcasttime");
        }
        private void OnRecSpellSelect(IServerPlayer fromPlayer, CTS_SelectPacket pack)
        {
            fromPlayer.Entity.Attributes.SetString("runespellselected", pack.spellID);
        }
        public override void Start(ICoreAPI api)
        {
            Runelogger = api.Logger;


            AllSpells = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BaseRuneSpell>>("runespells").Recipes;
            AltarRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BaseRuneAltar>>("runealtar").Recipes;


            if (api.Side == EnumAppSide.Client)
            {
                capi_Runechannel = (api as ICoreClientAPI).Network.RegisterChannel("runespellchannel")
                    .RegisterMessageType(typeof(CTS_SpellPacket))
                    .RegisterMessageType(typeof(CTS_SelectPacket));
            }
            else
            {
                runeSApi = api as ICoreServerAPI;
                sapi_Runechannel = (api as ICoreServerAPI).Network.RegisterChannel("runespellchannel")
                    .RegisterMessageType(typeof(CTS_SpellPacket))
                    .SetMessageHandler<CTS_SpellPacket>(OnRecCastRequest)
                    .RegisterMessageType(typeof(CTS_SelectPacket))
                    .SetMessageHandler<CTS_SelectPacket>(OnRecSpellSelect);
            }

            api.RegisterEntity("basesummonrunespell", typeof(defaultSpell));
            api.RegisterEntity("makestickspellent", typeof(MakeStickEnt));
            api.RegisterEntity("healotherspellent", typeof(HealOther));
            api.RegisterEntity("healselfspellent", typeof(HealSelf));
            api.RegisterEntity("windstorespellent", typeof(StoreItems));
            api.RegisterEntity("ignitespellspellent", typeof(Ignite));
            api.RegisterEntity("fertilizespellspellent", typeof(GrowSpell));
            api.RegisterEntity("elementalspellent", typeof(BasicElemental));
            api.RegisterEntity("grouphealspellent", typeof(HealAOE));

            api.RegisterBlockClass("runealtar-b", typeof(RuneAltarBlock));
            api.RegisterBlockEntityClass("runealtar-be", typeof(RuneAltarBe));
            api.RegisterBlockEntityBehaviorClass("runealtar-chiselsteal-bhv", typeof(BEBChiseledCover));
            api.RegisterBlockBehaviorClass("runealtar-chiselsteal-bb", typeof(BBChiseledCover));

            api.RegisterCollectibleBehaviorClass("runepouchbag", typeof(CollectibleRuneBag));

            api.Logger.Notification("RuneStory is coded by Len, god save us all.");
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += player => {
                player.Entity.Attributes.SetLong("runespellnextcasttime", 0); 
            };
            api.Logger.Notification("Runestory is running. Why would you do this to your poor server.");
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if(api is not ICoreServerAPI sApi) { return; }
            bool classExclu = sApi.World.Config.GetBool("classExclusiveRecipes", true);


            LoadSpells(sApi);
            LoadRuneAltars(sApi);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            runeGuiSys = api.ModLoader.GetModSystem<ImGuiModSystem>();
            runeGuiSys.Draw += theOneGui.Draw;
            runeGuiSys.Closed += theOneGui.Close;

            api.Logger.Notification("Runestory? On my client? Its more likely than you think!");

            api.Assets.AddModOrigin("runestory", "textures/spellicons");
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
                    for (int i = 0;i < jarray.Count;i++)
                    {
                        JToken tok = jarray.ElementAt(i);
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

            api.Logger.Log(EnumLogType.Debug, $"Found {found} spells, loaded {loaded}, and {failed} failed.");
            api.Logger.Log(EnumLogType.StoryEvent, "The Rise into the Fifth Age...");
        }

        public void LoadSpell<T>(ICoreServerAPI api, AssetLocation assLoc, BaseRuneSpell spell, ref int loaded, ref int failed) where T : BaseRuneI<T>
        {
            if (!spell.Enabled) return;
            if (spell.Code == null) { spell.Code = assLoc; }
            if(!spell.Resolve(api.World, "RuneStory Spell Resolver"))
            {
                failed++;
            }

            AllSpells.Add(spell);
            loaded++;
        }
        #region RuneAltar
        public void LoadRuneAltars(ICoreServerAPI api)
        {
            Dictionary<AssetLocation, JToken> loadedSpells = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/runealtar");

            int loaded = 0;
            int failed = 0;
            int found = 0;
            foreach ((AssetLocation path, JToken jboi) in loadedSpells)
            {
                if (jboi is JObject yepjson)
                {

                    BaseRuneAltar? parsed = yepjson.ToObject<BaseRuneAltar>(path.Domain);

                    if (parsed == null)
                    {
                        api.Logger.Error($"Failed parsing Altar Recipe at {path}");
                        continue;
                    }
                    LoadRuneAltar<BaseRuneAltar>(api, path, parsed, ref loaded, ref failed);
                    found++;

                }
                else if (jboi is JArray jarray)
                {
                    foreach (JToken tok in jarray)
                    {
                        BaseRuneAltar? parsed = tok.ToObject<BaseRuneAltar>(path.Domain);

                        if (parsed == null)
                        {
                            api.Logger.Error($"Failed parsing Altar Recipe at {path}");
                            continue;
                        }
                        LoadRuneAltar<BaseRuneAltar>(api, path, parsed, ref loaded, ref failed);
                        found++;
                    }
                }
            }

            api.Logger.Log(EnumLogType.Debug, $"Found {found} altar recipes, loaded {loaded}, and {failed} failed.");
            api.Logger.Log(EnumLogType.StoryEvent, "The Fall from the First Age...");
        }

        public void LoadRuneAltar<T>(ICoreServerAPI api, AssetLocation assLoc, BaseRuneAltar spell, ref int loaded, ref int failed) where T : BaseRuneAltarI<T>
        {
            if (!spell.Enabled) return;
            if (spell.Code == null) { spell.Code = assLoc; }
            if (!spell.Resolve(api.World, "RuneStory Spell Resolver"))
            {
                failed++;
            }

            AltarRecipes.Add(spell);
            loaded++;
        }
        #endregion
        public override void Dispose()
        {
            theOneGui = null;
            base.Dispose();
        }
    }
}
