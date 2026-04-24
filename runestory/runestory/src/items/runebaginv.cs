using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace runestory
{

    public class RuneItemSlot : ItemSlotBagContent
    {
        public RuneItemSlot(InventoryBase inventory, int BagIndex, int SlotIndex, EnumItemStorageFlags storageType) : base(inventory, BagIndex, SlotIndex, storageType)
        {
        }

        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            // 1. Current slot empty: Take items
            if (Empty)
            {
                if (!CanHold(sourceSlot)) return;

                int q = Math.Min(sourceSlot.StackSize, MaxSlotStackSize);
                q = Math.Min(q, GetRemainingSlotSpace(sourceSlot.Itemstack));

                itemstack = sourceSlot.TakeOut(q);
                op.MovedQuantity = itemstack.StackSize;
                OnItemSlotModified(itemstack);
                return;
            }

            // 2. Current slot non empty, source slot empty: Put items
            if (sourceSlot?.Empty ?? true)
            {
                op.RequestedQuantity = 64;
                TryPutInto(sourceSlot, ref op);
                return;
            }
             
            // 3. Both slots not empty, and they are stackable: Fill slot
            int maxq = (itemstack.Collectible.MaxStackSize*2) - itemstack.StackSize;
            if (maxq > 0 && itemstack.Collectible.Code == sourceSlot?.Itemstack?.Collectible?.Code) 
            {
                int tomove = Math.Min(maxq,GetRemainingSlotSpace(sourceSlot.Itemstack));
                op.RequestedQuantity = tomove;

                ItemStackMergeOperation mergeop = op.ToMergeOperation(this, sourceSlot);
                op = mergeop;

                int moving = Math.Min(sourceSlot.Itemstack.StackSize, tomove);
                itemstack.StackSize += moving;
                sourceSlot.TakeOut(moving);
                if(sourceSlot.Itemstack?.StackSize <=0)
                {
                    sourceSlot.TakeOutWhole();
                }

                sourceSlot.OnItemSlotModified(itemstack);
                OnItemSlotModified(itemstack);

                op.RequestedQuantity = tomove; //ensures op.NotMovedQuantity will be correct in calling code if used with slots with limited slot maxStackSize, e.g. InventorySmelting with a cooking container has slots with maxStackSize == 6
                return;
            }


            if(CanTake()&& sourceSlot.Empty)
            {
                int tomove = Math.Min(sourceSlot.StackSize,itemstack.Collectible.MaxStackSize);

                sourceSlot.TakeOut(tomove);
                if(sourceSlot.StackSize <= 0)
                {
                    sourceSlot.TakeOutWhole();
                }
            }
            
        }

        public override int GetRemainingSlotSpace(ItemStack forItemstack)
        {
            if (WildcardUtil.Match("runestory:rune-*", forItemstack.Collectible.Code.ToString()))
            {
                return forItemstack.Collectible.MaxStackSize * 2;
            }
            return 0;
        }
    }
    public class CollectibleRuneBag : CollectibleBehavior,IHeldBag,IAttachedInteractions
    {
        public const int PacketIdBitShift = 11;    // magic number; see also IClientNetworkAPI.SendEntityPacketWithOffset() which enables such tricks

        public TagSet StorageTags;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            var tags = properties["tags"].Token;
            if (tags != null) StorageTags = CollectibleTagSetConverter.ProxyInstance.ReadJson(tags);
        }

        public void Clear(ItemStack backpackStack)
        {
            ITreeAttribute stackBackpackTree = backpackStack.Attributes.GetTreeAttribute("backpack");
            stackBackpackTree["slots"] = new TreeAttribute();
        }

        public ItemStack[] GetContents(ItemStack bagstack, IWorldAccessor world)
        {
            ITreeAttribute backpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            if (backpackTree == null) return null;

            List<ItemStack> contents = new List<ItemStack>();
            ITreeAttribute slotsTree = backpackTree.GetTreeAttribute("slots");

            foreach (var val in slotsTree.SortedCopy())
            {
                ItemStack cstack = (ItemStack)val.Value?.GetValue();

                if (cstack != null)
                {
                    cstack.ResolveBlockOrItem(world);
                }

                contents.Add(cstack);
            }

            return contents.ToArray();
        }

        public virtual bool IsEmpty(ItemStack bagstack)
        {
            ITreeAttribute backpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            if (backpackTree == null) return true;
            ITreeAttribute slotsTree = backpackTree.GetTreeAttribute("slots");

            foreach (var val in slotsTree)
            {
                IItemStack stack = (IItemStack)val.Value?.GetValue();
                if (stack != null && stack.StackSize > 0) return false;
            }

            return true;
        }

        public virtual int GetQuantitySlots(ItemStack bagstack)
        {
            if (bagstack == null || bagstack.Collectible.Attributes == null) return 0;
            return bagstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
        }

        public void Store(ItemStack bagstack, ItemSlotBagContent slot)
        {
            ITreeAttribute stackBackpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            ITreeAttribute slotsTree = stackBackpackTree.GetTreeAttribute("slots");

            slotsTree["slot-" + slot.SlotIndex] = new ItemstackAttribute(slot.Itemstack);
        }

        public virtual string GetSlotBgColor(ItemStack bagstack)
        {
            return bagstack.ItemAttributes["backpack"]["slotBgColor"].AsString(null);
        }

        public const int defaultFlags = (int)(EnumItemStorageFlags.General | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Outfit);

        public CollectibleRuneBag(CollectibleObject collObj) : base(collObj)
        {
        }

        public virtual EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
        {
            return (EnumItemStorageFlags)bagstack.ItemAttributes["backpack"]["storageFlags"].AsInt(defaultFlags);
        }

        public virtual TagSet GetStorageTags(ItemStack bagStack)
        {
            return StorageTags;
        }

        public List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
        {
            var bagContents = new List<ItemSlotBagContent>();

            string bgcolhex = GetSlotBgColor(bagstack);
            var flags = GetStorageFlags(bagstack);
            int quantitySlots = GetQuantitySlots(bagstack);
            TagSet storageTags = GetStorageTags(bagstack);

            ITreeAttribute stackBackpackTree = bagstack.Attributes.GetTreeAttribute("backpack");
            if (stackBackpackTree == null)
            {
                stackBackpackTree = new TreeAttribute();
                ITreeAttribute slotsTree = new TreeAttribute();

                for (int slotIndex = 0; slotIndex < quantitySlots; slotIndex++)
                {
                    ItemSlotBagContent slot = new RuneItemSlot(parentinv, bagIndex, slotIndex, flags);
                    slot.HexBackgroundColor = bgcolhex;
                    slot.CanStoreTags = storageTags;
                    bagContents.Add(slot);
                    slotsTree["slot-" + slotIndex] = new ItemstackAttribute(null);
                }

                stackBackpackTree["slots"] = slotsTree;
                bagstack.Attributes["backpack"] = stackBackpackTree;
            }
            else
            {
                ITreeAttribute slotsTree = stackBackpackTree.GetTreeAttribute("slots");

                foreach (var val in slotsTree)
                {
                    int slotIndex = val.Key.Split("-")[1].ToInt();
                    ItemSlotBagContent slot = new RuneItemSlot(parentinv, bagIndex, slotIndex, flags);
                    slot.HexBackgroundColor = bgcolhex;
                    slot.CanStoreTags = storageTags;

                    if (val.Value?.GetValue() != null)
                    {
                        ItemstackAttribute attr = (ItemstackAttribute)val.Value;
                        slot.Itemstack = attr.value;
                        slot.Itemstack.ResolveBlockOrItem(world);
                    }

                    while (bagContents.Count <= slotIndex) bagContents.Add(null);
                    bagContents[slotIndex] = slot;
                }
            }

            return bagContents;
        }
        public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity)
        {

        }

        public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
        {
            getOrCreateContainerWorkspace(slotIndex, fromEntity, null).Close((byEntity as EntityPlayer).Player);
        }


        public AttachedContainerWorkspace getOrCreateContainerWorkspace(int slotIndex, Entity onEntity, Action onRequireSave)
        {
            return ObjectCacheUtil.GetOrCreate(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id, () => new AttachedContainerWorkspace(onEntity, onRequireSave));
        }

        public AttachedContainerWorkspace getContainerWorkspace(int slotIndex, Entity onEntity)
        {
            return ObjectCacheUtil.TryGet<AttachedContainerWorkspace>(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id);
        }


        public virtual void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
        {
            var controls = byEntity.MountedOn?.Controls ?? byEntity.Controls;
            if (!controls.CtrlKey)
            {
                handled = EnumHandling.PreventDefault;
                if (onEntity.Api.Side == EnumAppSide.Client)
                {
                    var workspace = getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave);
                    workspace.OnInteract(bagSlot, slotIndex, onEntity, byEntity, hitPosition);
                }
            }
        }

        public void OnReceivedClientPacket(ItemSlot bagSlot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave)
        {
            int targetSlotIndex = packetid >> PacketIdBitShift;

            if (slotIndex != targetSlotIndex) return;

            int first10Bits = (1 << PacketIdBitShift) - 1;
            packetid = packetid & first10Bits;

            getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave).OnReceivedClientPacket(player, packetid, data, bagSlot, slotIndex, ref handled);
        }

        public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity)
        {
            return true;
        }

        public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity fromEntity)
        {
            return IsEmpty(itemslot.Itemstack);
        }

        public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn)
        {
            if (despawn.Reason == EnumDespawnReason.Death)
            {
                var contents = GetContents(itemslot.Itemstack, onEntity.World);
                if (contents != null)
                {
                    foreach (var stack in contents)
                    {
                        if (stack == null) continue;
                        onEntity.World.SpawnItemEntity(stack, onEntity.Pos.XYZ);
                    }
                }
            }

            getContainerWorkspace(slotIndex, onEntity)?.OnDespawn(despawn);
        }

        public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath)
        {

        }
    }
}
