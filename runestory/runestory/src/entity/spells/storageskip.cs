using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace runestory.src.entity.spells
{
    public class StoreItems : BaseRuneEnt
    {
        public override void OnTouchEntity(Entity entity)
        {
            SendItems(entity);
            Die();
        }

        public override void OnCollided()
        {
            SendItems(Api.World.GetNearestEntity(Pos.XYZ, 1, 1));
            Die();
        }

        public void SendItems(Entity entity)
        {
            if(Api.Side == EnumAppSide.Client || spawnedBy is null) { return; }
            if (spawnedBy is EntityPlayer ply && ply.Controls.ShiftKey)
            {
                if (ply.BlockSelection?.Position is null) { return; }
                spawnedBy.Attributes.SetInt("senditems-x", ply.BlockSelection.Position.X);
                spawnedBy.Attributes.SetInt("senditems-y", ply.BlockSelection.Position.Y);
                spawnedBy.Attributes.SetInt("senditems-z", ply.BlockSelection.Position.Z);
                (ply.Player as IServerPlayer).SendMessage(GlobalConstants.GeneralChatGroup,$"You mentally attune your mind to send items to cordinates {ply.BlockSelection.Position.X}, {ply.BlockSelection.Position.Y}, {ply.BlockSelection.Position.Z}.",EnumChatType.Notification);
                for(int i =0;i< ourSpell.Reagents.Count;i++)
                {
                    ply.TryGiveItemStack(new(World.GetItem(ourSpell.Reagents.ElementAt(i).Key),ourSpell.Reagents.ElementAt(i).Value));
                }
            }
            else
            {
                int x = spawnedBy.Attributes.GetInt("senditems-x");
                int y = spawnedBy.Attributes.GetInt("senditems-y");
                int z = spawnedBy.Attributes.GetInt("senditems-z");

                if (x == 0 && y == 0 && z == 0)
                {
                    return;
                }

                BlockPos target = new(x, y, z);

                Entity[] nearHit = Api.World.GetEntitiesAround(Pos.XYZ, 2, 2, entity => entity is EntityItem);

                if (Api.World.BlockAccessor.GetBlockEntity(target) is BlockEntityContainer cont)
                {
                    foreach (Entity ei in nearHit)
                    {
                        EntityItem item = (ei as EntityItem);
                        EntityItemSlot temp = new EntityItemSlot(item);
                        temp.Itemstack = item.Itemstack.Clone();
                        foreach(ItemSlot slot in cont.Inventory)
                        {
                            ItemStackMoveOperation mov = new(Api.World, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.DirectMerge,temp.StackSize);
                            int moved = MOVE(slot, temp, ref mov);
                            if ( moved == 0 ) { continue; }
                            item.Itemstack.StackSize -= moved;
                            if (item.Itemstack.StackSize <= 0)
                            {
                                item.Die();
                            }
                            break;
                        }
                    }
                    Api.World.BlockAccessor.GetChunkAtBlockPos(target)?.MarkModified();
                }
                else
                {
                    foreach (EntityItem item in nearHit)
                    {
                        item.TeleportTo(target);
                    }
                    Api.World.BlockAccessor.GetChunkAtBlockPos(target)?.MarkModified();
                }
            }
        }
        public virtual int MOVE(ItemSlot sinkSlot,ItemSlot toslot, ref ItemStackMoveOperation op)
        {
            if (sinkSlot.Itemstack == null)
            {
                int num = Math.Min(sinkSlot.GetRemainingSlotSpace(toslot.Itemstack), op.RequestedQuantity);
                if (num > 0)
                {
                    sinkSlot.Itemstack = toslot.TakeOut(num);
                    op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, num));
                    sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
                    toslot.OnItemSlotModified(sinkSlot.Itemstack);
                }

                return op.MovedQuantity;
            }

            ItemStackMergeOperation itemStackMergeOperation = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, toslot));
            int requestedQuantity = op.RequestedQuantity;
            op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(toslot.Itemstack), op.RequestedQuantity);
            sinkSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
            if (itemStackMergeOperation.MovedQuantity > 0)
            {
                sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
                toslot.OnItemSlotModified(sinkSlot.Itemstack);
            }

            op.RequestedQuantity = requestedQuantity;
            return itemStackMergeOperation.MovedQuantity;
        }
    }
}
