
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class Redstone : BlockEntityBehavior
    {
        private IRedstoneTaker? redTaker;
        private IRedstoneSender? redSender;
        private bool dirtyboi = true;
        private string frequency = "";
        private string outfrequency = "";

        public string Frequency
        {
            get => frequency; set
            {
                if (frequency != value)
                {
                    frequency = value;
                    dirtyboi = true;
                    begin();
                }
            }
        }
        public string OutFrequency
        {
            get => outfrequency; set
            {
                if (outfrequency != value)
                {
                    outfrequency = value;
                    dirtyboi = true;
                    begin();
                }
            }
        }

        public Redstone(BlockEntity blockentity) : base(blockentity)
        {
        }
        public global::LensstoryMod.LensMachinationsMod? System => this.Api?.ModLoader.GetModSystem<global::LensstoryMod.LensMachinationsMod>();
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            begin(true);
        }
        public void begin(bool forced = false)
        {
            if (dirtyboi || forced)
            {
                var thesys = this.System;
                if (thesys is not null)
                {
                    dirtyboi = false;

                    redTaker = null;
                    redSender = null;

                    foreach (var behavior in this.Blockentity.Behaviors)
                    {
                        switch (behavior)
                        {
                            case IRedstoneTaker { } consumer:
                                this.redTaker = consumer;
                                break;
                            case IRedstoneSender { } maker:
                                this.redSender = maker;
                                break;
                        }
                    }
                    if (redTaker is not null) 
                    {
                        System.SetRedTaker(Blockentity.Pos, redTaker);
                    }
                    if (redSender is not null)
                    {
                        System.SetRedProducer(Blockentity.Pos, redSender);
                    }
                    if (thesys.DoRedUpdate(Blockentity.Pos, frequency, true) || forced)
                    {
                        Blockentity.MarkDirty(true);
                    }
                    if (thesys.DoOutRedUpdate(Blockentity.Pos, outfrequency, true) || forced)
                    {
                        Blockentity.MarkDirty(true);
                    }
                }
                else
                {
                    dirtyboi = true;
                }
            }
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            System?.RedRemove(Blockentity.Pos);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            var mananetInfo = System?.GetRedNetInfo(Blockentity.Pos);

            dsc.AppendLine("RedNet:")
                .AppendLine("InNetID: " + mananetInfo?.NetworkID)
                .AppendLine("OutNetID: " + mananetInfo?.OutNetworkID);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetString("frequency", frequency);
            tree.SetString("outfreqency", outfrequency);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            var freq = tree.GetString("frequency");

            if (Frequency != freq)
            {
                Frequency = freq;
                dirtyboi = true;
            }
            var outfreq = tree.GetString("outfreqency");

            if(OutFrequency != outfreq)
            {
                OutFrequency = outfreq;
                dirtyboi = true;
            }
            begin(true);
        }

    }

    public interface IRedstoneSender
    {
        public bool Active();
    }

    public interface IRedstoneTaker
    {
        public void OnSignal(bool Activated);
    }

    public class RedstoneTaker
    {
        public readonly IRedstoneTaker Taker;

        public RedstoneTaker(IRedstoneTaker ITaker)
        {
            Taker = ITaker;
        }
    }

}
