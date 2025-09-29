using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectricalProgressive;
using ElectricalProgressive.Content.Block;
using ElectricalProgressive.Content.Block.EAccumulator;
using ElectricalProgressive.Interface;
using ElectricalProgressive.Utils;
using VintageEngineering;
using VintageEngineering.Electrical;
using VintageEngineering.Electrical.Systems.Catenary;
using VintageEngineering.Electrical.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;
using HarmonyLib;
using Vintagestory.API.Util;

namespace EpxVe.src
{
    public class BEBhvConverterBase : BlockEntityBehavior,IElectricalBlockEntity
    {

        protected BEBehaviorElectricalProgressive EPSys => Blockentity.GetBehavior<BEBehaviorElectricalProgressive>();

        public float MaxEn = 1000f; //Todo: Move to block property?

        public float RealPow;

        
        public ulong MaxPower => (ulong)MaxEn;

        public ulong MaxPPS => 100;

        public bool CanReceive;

        public bool CanExtract;
        public ulong CurrentPower => (ulong)RealPow;
        public EnumElectricalEntityType ElecType;

        public EnumElectricalEntityType ElectricalEntityType => ElecType;

        public bool IsPowerFull => RealPow >= MaxPower;

        public bool IsSleeping => false;

        public bool IsEnabled => true;

        public int Priority => 1;

        public bool IsLoaded { get; set; } = false;

        protected Dictionary<int, List<WireNode>> electricConnections = null;

        protected Dictionary<int, long> NetworkIDs = null;

        bool IElectricalBlockEntity.CanReceivePower => CanReceive;

        bool IElectricalBlockEntity.CanExtractPower => CanExtract;

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            CanReceive = Blockentity.GetBehavior<ElectricBEBehavior>().properties["canReceivePower"].AsBool();
            CanExtract = Blockentity.GetBehavior<ElectricBEBehavior>().properties["canExtractPower"].AsBool();
            ElecType = Enum.Parse<EnumElectricalEntityType>(Blockentity.GetBehavior<ElectricBEBehavior>().properties["entitytype"].AsString());

            IsLoaded = true;

            if (electricConnections == null) electricConnections = new Dictionary<int, List<WireNode>>();
            if (NetworkIDs == null) NetworkIDs = new Dictionary<int, long>();
            if (NetworkIDs.Count > 0 && electricConnections.Count > 0 && api.Side == EnumAppSide.Server)
            {
                ElectricalNetworkManager nm = api.ModLoader.GetModSystem<ElectricalNetworkMod>(true).manager;
                if (nm != null)
                {
                    foreach (KeyValuePair<int, long> networkpair in NetworkIDs)
                    {
                        if (networkpair.Value == 0)
                        {
                            NetworkIDs.Remove(networkpair.Key);
                            continue;
                        }
                        if (Block is WiredBlock wiredBlock)
                        {
                            if (wiredBlock.WireAnchors == null) continue;
                            WireNode node = wiredBlock.GetWireNodeInBlock(networkpair.Key).Copy();
                            if (node == null) continue;
                            node.blockPos = Pos.Copy();           
                            nm.JoinNetwork(networkpair.Value, node, this);
                        }
                    }
                }
            }
        }
        public BEBhvConverterBase(BlockEntity blockentity) : base(blockentity)
        {
        }

        public ulong RatedPower(float dt, bool isInsert = false)
        {
            ulong rate = (ulong)Math.Round(MaxPPS * dt);
            if (isInsert)
            {
                ulong emptycap = (ulong)(MaxPower - RealPow);
                return emptycap < rate ? emptycap : rate;
            }
            else
            {
                return (ulong)(RealPow < rate ? RealPow : rate);
            }
        }
        public void Update()
        {
            Blockentity.MarkDirty();
        }
        #region StolenFromElectricalBEBehavior
        public ulong ReceivePower(ulong powerOffered, float dt = 1f, bool simulate = false)
        {
            if(!CanReceive) { return powerOffered; }
            if (RealPow >= MaxPower) return powerOffered;

            ulong pps = (ulong)Math.Round(MaxPPS * dt);

            if (pps == 0) pps = ulong.MaxValue;
            else pps += 2;

            ulong capacityempty = (ulong)(MaxPower - RealPow);

            pps = (pps > capacityempty) ? capacityempty : pps;
            if (pps >= powerOffered)
            {
                if (!simulate) RealPow += powerOffered;
                Blockentity.MarkDirty(true);
                return 0;
            }
            else
            {
                if (!simulate) RealPow += pps;
                Blockentity.MarkDirty(true);
                return powerOffered - pps;
            }
        }

        public ulong ExtractPower(ulong powerWanted, float dt = 1f, bool simulate = false)
        {
            if (!CanExtract) return powerWanted;
            if (RealPow == 0) return powerWanted;
            ulong pps = (ulong)Math.Round(MaxPPS * dt);
            if (pps == 0) pps = ulong.MaxValue;
            pps = (ulong)((pps > RealPow) ? RealPow : pps);

            if (pps >= powerWanted)
            {
                if (!simulate) RealPow -= powerWanted;
                Blockentity.MarkDirty(true);
                return 0;
            }
            else
            {
                if (!simulate) RealPow -= pps;
                Blockentity.MarkDirty(true);
                return powerWanted - pps;
            }
        }

        public void CheatPower(bool drain = false)
        {
            RealPow = drain ? MaxEn : 0f;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("currentpower", (long)RealPow);
            tree.SetBytes("connections", SerializerUtil.Serialize(electricConnections));
            tree.SetBytes("networkids", SerializerUtil.Serialize(NetworkIDs));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RealPow = (ulong)tree.GetLong("currentpower", 0);

            byte[] connections = tree.GetBytes("connections");
            if (connections != null)
            {
                electricConnections = SerializerUtil.Deserialize<Dictionary<int, List<WireNode>>>(tree.GetBytes("connections"));
            }
            else
            {
                electricConnections = new Dictionary<int, List<WireNode>>();
            }
            byte[] netids = tree.GetBytes("networkids");
            if (netids != null)
            {
                NetworkIDs = SerializerUtil.Deserialize<Dictionary<int, long>>(tree.GetBytes("networkids"));
            }
            else
            {
                NetworkIDs = new Dictionary<int, long>();
            }

        }

        #endregion StolenFromElectricalBEBehavior
    }

    public class BEBhvConverterToEP : BEBhvConverterBase, IElectricProducer
    {
        public BEBhvConverterToEP(BlockEntity blockentity) : base(blockentity)
        {
        }

        protected float OrderedPower;
        protected float GivingPower;
        public void Produce_order(float amount)
        {
            OrderedPower = amount;
        }

        public float Produce_give()
        {
            float amnt = Math.Min(CurrentPower, OrderedPower);
            GivingPower = amnt;
            ExtractPower((ulong)amnt);
            return amnt;
        }

        public float getPowerGive()
        {
            return GivingPower;
        }

        public float getPowerOrder()
        {
            return OrderedPower;
        }
    }
    public class BEBhvConverterToVE : BEBhvConverterBase, IElectricConsumer
    {
        public BEBhvConverterToVE(BlockEntity blockentity) : base(blockentity)
        {
        }

        protected float powerReceive;
        protected float powGetNeed;
        //I have no idea what this does and cba to find out right now.
        public float AvgConsumeCoeff { get; set; }

        public float Consume_request()
       {
           float powNeeded = Math.Max(Math.Min(MaxPower - RealPow, 32), 0);
           powGetNeed = powNeeded;
           return powNeeded;
       }

        public void Consume_receive(float amount)
        {
            powerReceive = amount;
            RealPow += amount;
            Blockentity.MarkDirty();
        }
        public float getPowerReceive()
        {
            return powerReceive;
        }

        public float getPowerRequest()
        {
            return powGetNeed;
        }
    }
}
