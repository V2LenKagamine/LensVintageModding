using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory.src.items
{
    public class RuneChisel : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if ((byEntity == null || byEntity.LeftHandItemSlot?.Itemstack?.Collectible?.GetTool(byEntity.LeftHandItemSlot) != EnumTool.Hammer) && (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
                handling = EnumHandHandling.PreventDefaultAction;
            }
            else if (!(blockSel?.Position == null))
            {
                BlockPos position = blockSel.Position;
                Block block = byEntity.World.BlockAccessor.GetBlock(position);
                ModSystemBlockReinforcement modSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
                if (modSystem != null && modSystem.IsReinforced(position))
                {
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                }
                else if (!byEntity.World.Claims.TryAccess(player, position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                }
                else if (blockSel == null)
                {
                    base.OnHeldInteractStart(slot,byEntity,blockSel,entitySel,firstEvent,ref handling);
                }

                if (WildcardUtil.Match("game:rock-*",block.Code.ToString()))
                {
                    string type = WildcardUtil.GetWildcardValue("game:rock-*",block.Code.ToString());
                    var boi = api.World.GetBlock("game:rockpolished-" + type);
                    if (boi is not null)
                    {
                        api.World.BlockAccessor.SetBlock(boi.Id, position);
                        if (player?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                        {
                            DamageItem(api.World, byEntity, slot);
                        }   
                        handling = EnumHandHandling.Handled;
                    }
                }

            }
        }
    }
}
