using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace LensstoryMod
{
    public class Blowdartgun : Item
    {
        private long nextshot;

        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            if (nextshot > 0)
            {
                nextshot--;
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
            return;
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            outputSlot.Itemstack.Attributes.SetFloat("lastshroomdmg",6f);
            base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (nextshot > 1) { return; }

            if(GetRemainingDurability(slot.Itemstack) <= 1 || byEntity.Controls.ShiftKey)
            {
                ItemSlot mushroom = null;
                byEntity.WalkInventory((invslot) =>
                {
                    if (invslot.Itemstack != null && invslot.Itemstack.Collectible.NutritionProps?.Health != null && invslot.Itemstack.Collectible.NutritionProps.Health < 0)
                    {
                        mushroom = invslot;
                        return false;
                    }
                    return true;
                });
                if(mushroom == null) { return; }
                int olddura = GetRemainingDurability(slot.Itemstack);
                if (olddura >= GetMaxDurability(slot.Itemstack)) { return; }
                slot.Itemstack.Attributes.SetFloat("lastshroomdmg", mushroom.Itemstack.Collectible.NutritionProps.Health);
                slot.Itemstack.Attributes.SetInt("durability", GetMaxDurability(slot.Itemstack)); ;
                slot.MarkDirty();
                mushroom.TakeOut(1);
                mushroom.MarkDirty();
                return;
            }
            float damage = 0;
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage += slot.Itemstack.Collectible.Attributes["attackpower"].AsFloat();
                if (slot.Itemstack.Attributes["lastshroomdmg"]!=null)
                {
                    float lastshroom = slot.Itemstack.Attributes.GetFloat("lastshroomdmg");
                    damage += (float)Math.Clamp(Math.Ceiling(Math.Abs(lastshroom))/2f,1f,5f);
                    if (Math.Abs(lastshroom) > 75)
                    {
                        damage += 3f;
                    }
                }
            }
            damage *= byEntity.Stats.GetBlended("rangedWeaponsDamage");
            EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("lensstory:blowdartprojectile"));
            var projectile = byEntity.World.ClassRegistry.CreateEntity(type) as EntitySimpleProjectile;
            projectile.FiredBy = byEntity;
            projectile.Damage = damage;

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw);
            Vec3d velocity = (aheadPos - pos) * 1.25f;

            projectile.ServerPos.SetPos(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0, byEntity.LocalEyePos.Y, 0));
            projectile.ServerPos.Motion.Set(velocity);
            projectile.Pos.SetFrom(projectile.ServerPos);
            projectile.World = byEntity.World;
            projectile.SetRotation();

            byEntity.World.SpawnEntity(projectile);

            nextshot = 40;
            if (GetRemainingDurability(slot.Itemstack) > 1) {
                slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot, 1);
            }
        }
    }
}
