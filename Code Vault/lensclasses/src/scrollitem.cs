using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace LensstoryMod
{
    public class ScrollItem : Item
    {
        public Dictionary<string,float> dic = new Dictionary<string, float>();
        public Dictionary<string, float> durdic = new Dictionary<string, float>();

        public string scrollID;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            JsonObject scroll = Attributes?["scrollinfo"];

            if(scroll?.Exists == true)
            {
                try
                {
                    scrollID = scroll["scrollID"].AsString();
                }
                catch (Exception e)
                {
                    api.World.Logger.Error("No idea what scroll ID is for scroll {0},Ignoring. Exception: {1}",Code,e);
                    scrollID = "";
                }
            }
            JsonObject effects = Attributes?["effects"];
            if (effects?.Exists == true)
            {
                try
                {
                    dic = effects.AsObject<Dictionary<string, float>>();
                }
                catch (Exception e)
                {
                    api.World.Logger.Error("No idea what scroll {0}'s effects are,Ignoring. Exception: {1}",Code,e);
                    dic.Clear();
                }
            }
            JsonObject durations = Attributes?["durations"];
            if(durations.Exists == true)
            {
                try
                {
                    durdic = durations.AsObject<Dictionary<string, float>>();
                }catch (Exception e)
                {
                    api.World.Logger.Error("No idea what scroll {0}'s durations are,Assuming endless. Exception: {1}", Code, e);
                    durdic.Clear();
                }
            }
        }

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return "eat";
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if(scrollID!= null && scrollID!= "")
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(byEntity.World is IClientWorldAccessor)
            {
                return secondsUsed <= 1.5f;
            }
            return true;
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Server && secondsUsed >= 1.5f)
            {
                if (scrollID.Contains("book"))
                {
                    foreach (var stat in dic)
                    {

                        var statmods = byEntity.Stats.Where(onent => stat.Key == onent.Key).Count();
                        var totalmod = byEntity.Stats.GetBlended(stat.Key) / statmods;
                        if (totalmod >= 1.5f || totalmod <= 0.5f)
                        {
                            IServerPlayer player = (byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer);
                            player.SendMessage(GlobalConstants.InfoLogChatGroup, "You feel like the " + slot.Itemstack.GetName() + " can't change you any further.", EnumChatType.Notification);
                            return;
                        }
                    }
                }
                ScrollEffect scrollboi = new ScrollEffect();
                scrollboi.ScrollStats((byEntity as EntityPlayer), dic, scrollID.Contains("book") ? "lensmod" : "lensmodtemp", scrollID, durdic);
                if (byEntity is EntityPlayer)
                {
                    IServerPlayer player = (byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer);

                    player.SendMessage(GlobalConstants.InfoLogChatGroup, "You feel the " + slot.Itemstack.GetName() + " alter your body.", EnumChatType.Notification);
                }
                slot.TakeOut(1);
                slot.MarkDirty();

            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if(dic != null)
            {
                dsc.AppendLine(Lang.Get("\n"));

                if(dic.ContainsKey("rangedWeaponsAcc"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% ranged accuracy {1}.", dic["rangedWeaponsAcc"] * 100,durdic.ContainsKey("rangedWeaponsAcc") ? Lang.Get("for {0} minutes",durdic["rangedWeaponsAcc"]) :"till death do you part"));
                }
                if (dic.ContainsKey("animalLootDropRate"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% bonus animal loot {1}.", dic["animalLootDropRate"] * 100,durdic.ContainsKey("animalLootDropRate") ? Lang.Get("for {0} minutes",durdic["animalLootDropRate"]) :"till death do you part"));
                }
                if (dic.ContainsKey("animalHarvestingTime"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% animal gather speed {1}.", dic["animalHarvestingTime"] * 100,durdic.ContainsKey("animalHarvestingTime") ? Lang.Get("for {0} minutes",durdic["animalHarvestingTime"]) :"till death do you part"));
                }
                if (dic.ContainsKey("animalSeekingRange"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% animal seek range {1}.", dic["animalSeekingRange"] * 100,durdic.ContainsKey("animalSeekingRange") ? Lang.Get("for {0} minutes",durdic["animalSeekingRange"]) :"till death do you part"));
                }
                if (dic.ContainsKey("forageDropRate"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% bonus forage {1}.", dic["forageDropRate"] * 100,durdic.ContainsKey("forageDropRate") ? Lang.Get("for {0} minutes",durdic["forageDropRate"]) :"till death do you part"));
                }
                if (dic.ContainsKey("healingeffectivness"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% healing effectiveness {1}.", dic["healingeffectivness"] * 100,durdic.ContainsKey("healingeffectivness") ? Lang.Get("for {0} minutes",durdic["healingeffectivness"]) :"till death do you part"));
                }
                if (dic.ContainsKey("hungerrate"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% hunger rate {1}.", dic["hungerrate"] * 100, durdic.ContainsKey("hungerrate") ? Lang.Get("for {0} minutes.",durdic["hungerrate"]):"till death do you part."));
                }
                if (dic.ContainsKey("meleeWeaponsDamage"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% melee damage {1}.", dic["meleeWeaponsDamage"] * 100,durdic.ContainsKey("meleeWeaponsDamage") ? Lang.Get("for {0} minutes",durdic["meleeWeaponsDamage"]) :"till death do you part"));
                }
                if (dic.ContainsKey("miningSpeedMul"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% mining speed {1}.", dic["miningSpeedMul"] * 100,durdic.ContainsKey("miningSpeedMul") ? Lang.Get("for {0} minutes",durdic["miningSpeedMul"]) :"till death do you part"));
                }
                if (dic.ContainsKey("oreDropRate"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% bonus ore {1}.", dic["oreDropRate"] * 100,durdic.ContainsKey("oreDropRate") ? Lang.Get("for {0} minutes",durdic["oreDropRate"]) :"till death do you part"));
                }
                if (dic.ContainsKey("rangedWeaponsDamage"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% ranged damage {1}.", dic["rangedWeaponsDamage"] * 100,durdic.ContainsKey("rangedWeaponsDamage") ? Lang.Get("for {0} minutes",durdic["rangedWeaponsDamage"]) :"till death do you part"));
                }
                if (dic.ContainsKey("rangedWeaponsSpeed"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% ranged speed {1}.", dic["rangedWeaponsSpeed"] * 100,durdic.ContainsKey("rangedWeaponsSpeed") ? Lang.Get("for {0} minutes",durdic["rangedWeaponsSpeed"]) :"till death do you part"));
                }
                if (dic.ContainsKey("walkspeed"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% walk speed {1}.", dic["walkspeed"] * 100,durdic.ContainsKey("walkspeed") ? Lang.Get("for {0} minutes",durdic["walkspeed"]) :"till death do you part"));
                }
                if (dic.ContainsKey("wildCropDropRate"))
                {
                    dsc.AppendLine(Lang.Get("When used, {0}% bonus wild crops {1}.", dic["wildCropDropRate"] * 100,durdic.ContainsKey("wildCropDropRate") ? Lang.Get("for {0} minutes",durdic["wildCropDropRate"]) :"till death do you part"));
                }
            }
        }

    }
}
