using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
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
        float powered;
        public float Powered { get { return powered; } set { if (powered != value) { powered = value; MarkDirty(); } } }

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

        public RefridgerationUnitBE()
        {
            container = new FrozenContainer(() => Inventory, "inventory")
            {
                owner = this
            };
        }

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null)
            {
                InitInventory(Block);
            }
            base.Initialize(api);
            RegisterGameTickListener(RecalulateNearby,30000); //30 seconds
        }

        public void RecalulateNearby(float _)
        {
            IBlockAccessor ba = Api.World.BlockAccessor;

            Powered = 0;
            float tempPower = 0;

            if ((float)Pos.Y / ba.MapSizeY <= 0.4f) { tempPower += 0.12f; }
            ba.WalkBlocks(Pos.AddCopy(-1,-1,-1), Pos.AddCopy(1,1,1), (block, _, __, ___) =>
            {
                switch (block.FirstCodePart())
                {
                    case "water": { tempPower += 0.02f; break; } 
                    case "lakeice":
                    case "glacierice": { tempPower += 0.04f; break; }
                    case "packedglacierice": { tempPower += 0.06f; break; }
                    default:
                        {
                            break;
                        }
                }
            });
            Powered = tempPower;
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
                    InitInventory(null);
                }
            }
            base.FromTreeAttributes(tree, worldForResolving);
            Powered = tree.GetFloat("coldness");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (Block != null) tree.SetInt("forBlockId", Block.BlockId);
            tree.SetFloat("coldness", Powered);
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
                var data = BlockEntityContainerOpen.ToBytes("BlockEntityInventory", Lang.Get(dialogTitleLangCode), 4, inventory);

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos,
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
            dsc.Append("Cold-ness: " + Math.Truncate(Powered*100) + "%");
        }

    }

    public class FrozenContainer : InWorldContainer
    {
        public RefridgerationUnitBE owner;
        public FrozenContainer(InventorySupplierDelegate inventorySupplier, string treeAttrKey) : base(inventorySupplier,treeAttrKey)
        {
        }
        public override float GetPerishRate()
        {
            switch (owner.Powered)
            {
                case float x when x <= 0:
                    {
                        return base.GetPerishRate();
                    }
                case float x when x > 0 && x < 1:
                    {
                        float perish = base.GetPerishRate();
                        return perish - (perish * owner.Powered);
                    }
                case float x when x >= 1:
                    {
                        return 0f;
                    }
            }
            return base.GetPerishRate();
        }
    }

}
