using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chiseltools.src;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ElectricProgressiveExtendedEquipment.src.items
{
    public class ElectricPalette : ItemPalette
    {
        int consperaction;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            consperaction = Attributes["perDurabilityDrain"] != null ? Attributes["perDurabilityDrain"].AsInt() : 20;
        }

        public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
        {
            int dura = itemslot.Itemstack.Attributes.GetInt("durability");
            if (dura > amount)
            {
                dura -= amount;
                itemslot.Itemstack.Attributes.SetInt("durability", dura);
            }
            else
            {
                itemslot.Itemstack.Attributes.SetInt("durability", 1);
            }
            itemslot.MarkDirty();
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("durability") * consperaction + "/" + inSlot.Itemstack.Collectible.GetMaxDurability(inSlot.Itemstack) * consperaction + " Power");
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (slot.Itemstack.Attributes.GetInt("durability") <= 1) { return; }
            handling = EnumHandHandling.PreventDefaultAction;
            if (blockSel == null)
            {
                return;
            }

            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }

            ModSystemBlockReinforcement modSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            if (modSystem != null && modSystem.IsReinforced(blockSel.Position))
            {
                player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }

            if (!(api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel blockEntityChisel))
            {
                TryChangeBlockToChisel(blockSel, byEntity, player);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }

            int paletteInk = GetPaletteInk(api, slot, usedurability: false);
            if (paletteInk == -1)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if (player.Entity.Controls.ShiftKey)
            {
                if (blockEntityChisel.BlockIds.Contains(paletteInk))
                {
                    return;
                }

                Block block = api.World.GetBlock(paletteInk);
                if (block == null || block.GetCollisionBoxes(api.World.BlockAccessor, blockSel.Position) == null)
                {
                    return;
                }

                api.World.PlaySoundAt(new AssetLocation("game:sounds/player/knap" + ((api.World.Rand.Next(2) > 0) ? 1 : 2)), player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z, player, randomizePitch: true, 12f);
                blockEntityChisel.AddMaterial(block);
                blockEntityChisel.AvailMaterialQuantities = new ushort[blockEntityChisel.BlockIds.Length];
                for (int i = 0; i < blockEntityChisel.BlockIds.Length; i++)
                {
                    blockEntityChisel.AvailMaterialQuantities[i] = 4096;
                }

                blockEntityChisel.MarkDirty();
            }
            else
            {
                Vec3i voxelHit = GetVoxelHit(blockSel);
                CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
                byte b = 0;
                foreach (uint voxelCuboid in blockEntityChisel.VoxelCuboids)
                {
                    BlockEntityMicroBlock.FromUint(voxelCuboid, cuboidWithMaterial);
                    if (cuboidWithMaterial.Contains(voxelHit.X, voxelHit.Y, voxelHit.Z))
                    {
                        b = cuboidWithMaterial.Material;
                        break;
                    }
                }

                if (blockEntityChisel.BlockIds[b] == paletteInk)
                {
                    handling = EnumHandHandling.PreventDefaultAction;
                    base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                    return;
                }

                List<int> list = new List<int>(blockEntityChisel.BlockIds);
                list[b] = paletteInk;
                blockEntityChisel.BlockIds = list.ToArray();
            }

            blockEntityChisel.MarkDirty(redrawOnClient: true);
            if (api is ICoreServerAPI && (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                int amount = 1;
                DamageItem(api.World, byEntity, player.InventoryManager.ActiveHotbarSlot, amount);
                player.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }

            handling = EnumHandHandling.PreventDefaultAction;
        }
        public static new int GetPaletteInk(ICoreAPI api, ItemSlot forslot, bool usedurability = true)
        {
            int result = -1;
            if (forslot == null || forslot.Itemstack == null || forslot.Itemstack.Item == null)
            {
                return -1;
            }

            if (forslot.Itemstack.Item != null && forslot.Itemstack.Item.Code.ToString().StartsWith("extendedelectrictools:epalette"))
            {
                if (GetPaletteBlockIds(api, forslot) == null)
                {
                    return result;
                }

                if (GetPaletteBlockIds(api, forslot).Length == 0)
                {
                    return result;
                }

                int num = Math.Min(GetPaletteBlockIds(api, forslot).Length - 1, forslot.Itemstack.Attributes.GetInt("toolMode"));
                if (usedurability)
                {
                    forslot.Itemstack.Collectible.DamageItem(api.World, null, forslot);
                    forslot.MarkDirty();
                }

                return GetPaletteBlockIds(api, forslot)[num];
            }

            return result;
        }
    }
}
