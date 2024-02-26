using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace LensstoryMod
{

    public class RefridgerationUnitBlock :BlockContainer
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is RefridgerationUnitBE entity)
            {
                return entity.OnPlayerRightClick(byPlayer,blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
    public class RefridgerationUnitBE : BlockEntityOpenableContainer,IBlockEntityContainer
    {
        bool powered;
        public bool Powered { get { return powered; } set { if (powered != value) { powered = value; MarkDirty(); } } }

        internal InventoryGeneric inventory;

        public int quantitySlots = 16;
        public string inventoryClassName = "lens-refridgerato";
        public string dialogTitleLangCode = "Icebox Contents";
        public bool retrieveOnly = false;

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        public override string InventoryClassName => inventoryClassName;

        public override float GetPerishRate()
        {
            if (Powered)
            {
                return 0f;
            }
            return base.GetPerishRate();
        }

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null)
            {
                InitInventory(Block);
            }

            base.Initialize(api);
            if(GetBehavior<Mana>()!=null)
            {
                GetBehavior<Mana>().begin(true);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
            {
                if (tree.HasAttribute("forBlockId"))
                {
                    InitInventory(worldForResolving.GetBlock((ushort)tree.GetInt("forBlockId")));
                }
                else
                {
                    ITreeAttribute inventroytree = tree.GetTreeAttribute("inventory");
                    int qslots = inventroytree.GetInt("qslots");
                    // Must be a basket
                    if (qslots == 8)
                    {
                        quantitySlots = 8;
                        inventoryClassName = "basket";
                        dialogTitleLangCode = "basketcontents";
                    }
                    InitInventory(null);
                }
            }
            base.FromTreeAttributes(tree, worldForResolving);

            if(GetBehavior<Mana>() != null)
            {
                GetBehavior<Mana>().begin(false);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            if (Block != null) tree.SetInt("forBlockId", Block.BlockId);
        }

        private void InitInventory(Block Block)
        {
            if (Block?.Attributes != null)
            {
                inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
                dialogTitleLangCode = Block.Attributes["dialogTitleLangCode"].AsString(dialogTitleLangCode);
                quantitySlots = Block.Attributes["quantitySlots"].AsInt(quantitySlots);
                retrieveOnly = Block.Attributes["retrieveOnly"].AsBool(false);
            }

            inventory = new InventoryGeneric(quantitySlots, null, null, null);

            if (Block.Attributes?["spoilSpeedMulByFoodCat"].Exists == true)
            {
                inventory.PerishableFactorByFoodCategory = Block.Attributes["spoilSpeedMulByFoodCat"].AsObject<Dictionary<EnumFoodCategory, float>>();
            }

            if (Block.Attributes?["transitionSpeedMul"].Exists == true)
            {
                inventory.TransitionableSpeedMulByType = Block.Attributes["transitionSpeedMul"].AsObject<Dictionary<EnumTransitionType, float>>();
            }

            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            inventory.SlotModified += OnSlotModifid;
        }

        private void OnSlotModifid(int slot)
        {
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }

        protected virtual void OnInvOpened(IPlayer player)
        {
            inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
        }

        protected virtual void OnInvClosed(IPlayer player)
        {
            inventory.PutLocked = retrieveOnly;
            invDialog?.Dispose();
            invDialog = null;
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.World is IServerWorldAccessor)
            {
                byte[] data;

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("BlockEntityInventory");
                    writer.Write(Lang.Get(dialogTitleLangCode));
                    writer.Write((byte)4);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockContainerPacketId.OpenInventory,
                    data
                );

                byPlayer.InventoryManager.OpenInventory(inventory);
            }

            return true;
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
        }

    }
    public class RefridgerationUnitBhv : BlockEntityBehavior, IManaConsumer
    {
        public RefridgerationUnitBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void EatMana(int mana)
        {
            if (Blockentity is RefridgerationUnitBE entity)
            {
                entity.Powered = mana >= ToVoid();
            }
        }

        public int ToVoid()
        {
            if( Blockentity is RefridgerationUnitBE boi)
            {
                return (int)Math.Round(boi.Inventory.Where(slot => slot.Itemstack != null).Count() / 4f) + 1;
            }
            return 0;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("MP:")
                .AppendLine("Consuming: " + ToVoid());
        }
    }
}
