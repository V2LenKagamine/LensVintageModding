using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace runestory
{
    public class defaultSpell : Entity
    {
        public bool freeCast = false;
        public string spellCode;
        public Entity spawnedBy;
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            if (spellCode != null)
            {
                BaseRuneSpell spell = Api.ModLoader.GetModSystem<RunestoryMS>().AllSpells.Find(poss => poss.Code == spellCode);
                EntityProperties? possible = Api.World.GetEntityType(new("runestory:" + spellCode));
                if(possible is EntityProperties resolved)
                {
                    BaseRuneEnt goodspell = Api.World.ClassRegistry.CreateEntity(resolved) as BaseRuneEnt;
                    if (spawnedBy != null)
                    {
                        if (!CheckReagents(spawnedBy, spell))
                        {
                            Die();
                            return;
                        }

                        goodspell.spawnedBy = spawnedBy;
                        goodspell.ourSpell = spell;
                        goodspell.freeCasted = freeCast;

                        Vec3d pos = spawnedBy.Pos.XYZ.AddCopy(0, spawnedBy.LocalEyePos.Y, 0);
                        Vec3d ahead = pos.AheadCopy(1, spawnedBy.Pos.Pitch, spawnedBy.Pos.Yaw);
                        Vec3d velo = (ahead - pos) * 0.55f;
                        //velo = new(0f, 0f, 0f);
                        goodspell.Pos.SetPos(spawnedBy.Pos.BehindCopy(0.21f).XYZ.Add(0, spawnedBy.LocalEyePos.Y, 0));
                        goodspell.Pos.Motion.Set(velo);
                        goodspell.World = spawnedBy.World;
                        goodspell.SetRotation();
                        World.PlaySoundAt(new AssetLocation("runestory:sounds/spellcast"),this,null,20f);
                        Api.World.SpawnPriorityEntity(goodspell);
                    }
                }
            }
            Die();
        }

        public bool CheckReagents(Entity spawner,BaseRuneSpell spell) 
        {
            if(spawner is EntityPlayer ply && spellCode != null)
            {
                if (ply.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) { return true; }
                
                int lookingAmt = spell?.Reagents?.Count ?? 0;
                Dictionary<ItemSlot, int> takeamnts = new(lookingAmt);
                for (int i = 0; i < lookingAmt; i++)
                {
                    string lookingfor = spell.Reagents.ElementAt(i).Key;
                    bool good = false;
                    ply.WalkInventory(slot => {
                        if(slot.Itemstack?.Collectible?.Code is null) { return true; }
                        bool returner = false;
                        int amtTake = spell.Reagents.ElementAt(i).Value;
                        if (lookingfor.Contains('*'))
                        {
                            returner = WildcardUtil.Match(lookingfor, slot.Itemstack.Collectible.Code.ToString()) && slot.Itemstack.StackSize >= amtTake;
                        }
                        else
                        {
                            returner = (slot.Itemstack.Collectible.Code.ToString() == lookingfor && slot.Itemstack.StackSize >= amtTake);
                        }
                        if(returner)
                        {
                            takeamnts.Add(slot,amtTake);
                            good = true;
                            return false;
                        }
                        return true;
                    });
                    if(!good) {
                        (Api.World.PlayerByUid(ply.PlayerUID) as IServerPlayer).SendMessage(GlobalConstants.GeneralChatGroup,Lang.Get("runestory:cast-fail"),EnumChatType.Notification);
                        return false; 
                    }
                }
                float noconsume = ply.Stats.GetBlended(RunestoryMS.RMS_Stat_RuneChance);
                if (World.Rand.NextDouble() < (noconsume)) {
                    freeCast = true;
                    foreach (var pair in takeamnts) {
                        pair.Key.TakeOut(pair.Value);
                        if (pair.Key.Itemstack?.StackSize <= 0)
                        {
                            pair.Key.TakeOutWhole();
                        }
                        pair.Key.MarkDirty();
                    }
                }
                return true;
            }
            return false;
        }
    }
}
