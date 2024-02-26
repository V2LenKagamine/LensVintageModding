
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class Mana : BlockEntityBehavior
    {
        private IManaConsumer? manaConsumer;
        private IManaMaker? manaMaker;
        private bool dirtyboi = true;
        private int manacode;

        public int ManaID { get => this.manacode; set {if (this.manacode != value)
                {
                    this.manacode = value;
                    this.dirtyboi = true;
                    this.begin();
                }
            } 
        }

        public Mana(BlockEntity blockentity) : base(blockentity)
        {
        }
        public global::LensstoryMod.LensMachinationsMod? System => this.Api?.ModLoader.GetModSystem<global::LensstoryMod.LensMachinationsMod>();
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            this.begin(true);
        }
        public void begin(bool forced = false) 
        {
            if( dirtyboi || forced)
            {
                var thesys = this.System;
                if(thesys is not null )
                {
                    this.dirtyboi = false;

                    this.manaConsumer = null;
                    this.manaMaker = null;

                    foreach (var behavior in this.Blockentity.Behaviors)
                    {
                        switch (behavior)
                        {
                            case IManaConsumer { } consumer:
                                this.manaConsumer = consumer;
                                break;
                            case IManaMaker { } maker: 
                                this.manaMaker = maker; 
                                break;
                                
                        }
                    }

                    System.SetManaConsumer(this.Blockentity.Pos, this.manaConsumer);
                    System.SetManaProducer(this.Blockentity.Pos, this.manaMaker);

                    if (thesys.DoUpdate(this.Blockentity.Pos,this.manacode,true) || forced)
                    {
                        this.Blockentity.MarkDirty(true);
                    }

                } else
                {
                    this.dirtyboi = true;
                }
            }
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            System?.Remove(Blockentity.Pos);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            var mananetInfo = this.System?.GetManaNetInfo(this.Blockentity.Pos);

            dsc.AppendLine("MP:")
                .AppendLine("NetID: " + mananetInfo?.ManaID)
                .AppendLine("Producers: " + mananetInfo?.TotalMakers)
                .AppendLine("Produced: " + mananetInfo?.ManaProduced)
                .AppendLine("Consumers: " + mananetInfo?.TotalConsumers)
                .AppendLine("Consumed: " + mananetInfo?.ManaConsumned);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("manacode",this.manacode);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            var manaid = tree.GetInt("manacode");

            if(this.manacode != manaid)
            {
                this.ManaID = manaid;
                this.dirtyboi = true;
            }
            this.begin(true);
        }

    }

    public interface IManaMaker
    {
        public int MakeMana();
    }

    public interface IManaConsumer
    {
        public int ToVoid();
        public void EatMana(int mana);
    }

    public class ManaConsumer
    {
        public readonly IManaConsumer ManaEater;
        public int ManaNeeded;

        public ManaConsumer(IManaConsumer manaEater)
        {
            ManaEater = manaEater;
            ManaNeeded = manaEater.ToVoid();
        }
    }

}
