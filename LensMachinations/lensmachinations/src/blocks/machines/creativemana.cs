
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace LensstoryMod
{
    public class CreativeMana : Block
    {

    }
    public class CreativeManaBE : BlockEntity
    {
        private int ManaNumber;

        private Mana theMana => this.GetBehavior<Mana>();
        public int ManaID { get => this.ManaNumber;set {
                if (value != this.ManaNumber) { }
                this.theMana.ManaID = ManaNumber = value;
                this.MarkDirty();
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            GetBehavior<Mana>().begin(true);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("manaid",ManaNumber);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            ManaNumber = tree.GetAsInt("manaid");
        }

    }
    public class CreativeManaBhv : BlockEntityBehavior,IManaMaker
    {

        public CreativeManaBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public int MakeMana()
        {
            return 99999;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("MP:")
                .AppendLine("Producing: " + 99999);
        }

    }

}
