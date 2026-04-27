using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using runestory.src.entity.spells;
using runestory.src.items;
using runestory.src.recipestuff;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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

        public static string RMS_SpellKnowledge => "RMSKnownSpells";
        public static string RMS_Stat_RuneChance => "RuneConsumeChance";
        public static string RMS_Stat_MagicDamage => "magicWeaponsDamage";

        private Harmony RMSHarmony;

        public List<BaseRuneSpell> AllSpells = [];
        public List<BaseRuneAltar> AltarRecipes = [];
        public List<BaseRefineRecipe> RefineRecipes = [];

        /* Quick reference to all attributes that change the characters Stats:
         *  healingeffectivness, maxhealthExtraPoints, walkSpeed, hungerrate, rangedWeaponsAcc, rangedWeaponsSpeed
         *rangedWeaponsDamage, meleeWeaponsDamage, mechanicalsDamage, animalLootDropRate, forageDropRate, wildCropDropRate
         *vesselContentsDropRate, oreDropRate, rustyGearDropRate, miningSpeedMul, animalSeekingRange, armorDurabilityLoss,
         *bowDrawingStrength, wholeVesselLootChance, temporalGearTLRepairCost, animalHarvestingTime
         */
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
            //Todo: Config?
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
            RefineRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BaseRefineRecipe>>("refinespell").Recipes;


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
            api.RegisterEntity("rockblastspellent", typeof(BlastSpell));
            api.RegisterEntity("superheatspellent", typeof(SuperHeat));
            api.RegisterEntity("foragespellspellent", typeof(ForageSpell));
            api.RegisterEntity("windfeetspellent", typeof(WindFeet));
            api.RegisterEntity("dwarfblessspellent", typeof(DwarfBlessing));
            api.RegisterEntity("watercloakspellent", typeof(WaterCloak));
            api.RegisterEntity("magelightspellent", typeof(MageLight));
            api.RegisterEntity("refinespellspellent", typeof(RefineItemSpell));

            api.RegisterBlockClass("runealtar-b", typeof(RuneAltarBlock));
            api.RegisterBlockEntityClass("runealtar-be", typeof(RuneAltarBe));
            api.RegisterBlockEntityBehaviorClass("runealtar-chiselsteal-bhv", typeof(BEBChiseledCover));
            api.RegisterBlockBehaviorClass("runealtar-chiselsteal-bb", typeof(BBChiseledCover));

            api.RegisterCollectibleBehaviorClass("runepouchbag", typeof(CollectibleRuneBag));
            api.RegisterItemClass("runicpickaxeitem", typeof(RunePickaxe));
            api.RegisterItemClass("runemagicresearchclass", typeof(RunicResearch));

            api.Logger.Notification("RuneStory is coded by Len, god save us all.");
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += player => {
                player.Entity.Attributes.SetLong("runespellnextcasttime", 0); 
            };
            api.Event.PlayerNowPlaying += (IServerPlayer player) =>
            {
                if (player.Entity is EntityPlayer)
                {
                    Entity e = player.Entity;
                    e.AddBehavior(new PlayerTempBuffer(e));
                    e.Attributes.TryGetAttribute(RMS_SpellKnowledge, out IAttribute spellsmaybe);
                    if (spellsmaybe is null)
                    {
                        string[] defspells = [];
                        foreach (var spll in AllSpells.Where(spell => spell.spellTier == 1))
                        {
                            defspells = defspells.AddToArray(spll.Code);
                        }

                        e.WatchedAttributes.SetAttribute(RMS_SpellKnowledge, new StringArrayAttribute(defspells));
                    }
                    if(!(e.Stats.Where(stat => stat.Key == RMS_Stat_MagicDamage).Any()))
                    {
                        //e.Stats.Register(RMS_Stat_MagicDamage);
                        e.Stats.Set(RMS_Stat_MagicDamage, "base", 1f, true);
                    }
                    if (!(e.Stats.Where(stat => stat.Key == RMS_Stat_RuneChance).Any()))
                    {
                        //e.Stats.Register(RMS_Stat_RuneChance);
                        e.Stats.Set(RMS_Stat_RuneChance, "base", 1f, true);
                    }
                }
            };
            api.Logger.Notification("Runestory is running. Why would you do this to your poor server.");
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if(api is not ICoreServerAPI sApi) { return; }
            bool classExclu = sApi.World.Config.GetBool("classExclusiveRecipes", true);

            LoadSpells(sApi);
            LoadRuneAltars(sApi);
            LoadRefineRecipes(sApi);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            runeGuiSys = api.ModLoader.GetModSystem<ImGuiModSystem>();
            runeGuiSys.Draw += theOneGui.Draw;
            runeGuiSys.Closed += theOneGui.Close;

            RMSHarmony = new Harmony("runestory");

            RMSHarmony.Patch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo)
                    .GetMethod(nameof(CollectibleBehaviorHandbookTextAndExtraInfo.GetHandbookInfo)),
                postfix: typeof(GetHandbookInfoPatch).GetMethod(
                    nameof(GetHandbookInfoPatch.Postfix)));

            RMSHarmony.Patch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo)
                    .GetMethod("addCreatedByInfo", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: typeof(AddCreatedByPatch).GetMethod(
                    nameof(AddCreatedByPatch.Postfix)));

            api.Logger.Notification("Runestory? On my client? Its more likely than you think!");

            api.Assets.AddModOrigin("runestory", "textures/spellicons");
        }
        public override void Dispose()
        {
            theOneGui = null;
            if (RMSHarmony is not null)
            {
                RMSHarmony.UnpatchAll("runestory");
            }
            base.Dispose();
        }
        #region Spells
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
        #endregion
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
        }
        public void LoadRuneAltar<T>(ICoreServerAPI api, AssetLocation assLoc, BaseRuneAltar spell, ref int loaded, ref int failed) where T : BaseRuneAltarI<T>
        {
            if (!spell.Enabled) return;
            if (spell.Code == null) { spell.Code = assLoc; }
            if (!spell.Resolve(api.World, "RuneStory Altar Resolver"))
            {
                failed++;
            }

            AltarRecipes.Add(spell);
            loaded++;
        }
        #endregion
        #region RefineRecipes
        public void LoadRefineRecipes(ICoreServerAPI api)
        {
            Dictionary<AssetLocation, JToken> loadedRefines = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/refinespell");

            int loaded = 0;
            int failed = 0;
            int found = 0;
            foreach ((AssetLocation path, JToken jboi) in loadedRefines)
            {
                if (jboi is JObject yepjson)
                {

                    BaseRefineRecipe? parsed = yepjson.ToObject<BaseRefineRecipe>(path.Domain);

                    if (parsed == null)
                    {
                        api.Logger.Error($"Failed parsing Refine Recipe at {path}");
                        continue;
                    }
                    LoadRefineRecipe<BaseRefineRecipe>(api, path, parsed, ref loaded, ref failed);
                    found++;

                }
                else if (jboi is JArray jarray)
                {
                    foreach (JToken tok in jarray)
                    {
                        BaseRefineRecipe? parsed = tok.ToObject<BaseRefineRecipe>(path.Domain);

                        if (parsed == null)
                        {
                            api.Logger.Error($"Failed parsing Redine Recipe at {path}");
                            continue;
                        }
                        LoadRefineRecipe<BaseRefineRecipe>(api, path, parsed, ref loaded, ref failed);
                        found++;
                    }
                }
            }

            api.Logger.Log(EnumLogType.Debug, $"Found {found} Refine recipes, loaded {loaded}, and {failed} failed.");
        }
        public void LoadRefineRecipe<T>(ICoreServerAPI api, AssetLocation assLoc, BaseRefineRecipe spell, ref int loaded, ref int failed) where T : BaseRefineRecipeI<T>
        {
            if (!spell.Enabled) return;
            if (spell.Code == null) { spell.Code = assLoc; }
            if (!spell.Resolve(api.World, "Runestory Refine Resolver"))
            {
                failed++;
            }

            RefineRecipes.Add(spell);
            loaded++;
        }

        #endregion

    }
}
