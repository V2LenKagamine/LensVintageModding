using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace LensstoryMod
{
    public class MagmanatorBe : BlockEntity
    {
        private double LastTickTotalHours;
        private bool Fueled;
        private int heat;

        public bool Working
        {
            get => Fueled; set
            {
                if (Fueled != value)
                {
                    if (value && !Fueled)
                    {
                        MarkDirty();
                    }
                    Fueled = value;
                };
            }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            RegisterGameTickListener(OnCommonTick, 5000);

            GetBehavior<Mana>().begin(true);
        }

        public void OnCommonTick(float dt)
        {
            if(Fueled)
            {
                var hourspast = Api.World.Calendar.TotalHours - LastTickTotalHours;
                if(heat < 3000 ){ heat += (int)Math.Floor(hourspast * 400); }
               
            }else
            {
                var hourspast = Api.World.Calendar.TotalHours - LastTickTotalHours;
                if(heat > 0) { heat -= (int)Math.Floor(hourspast * 200); }
            }
            heat = Math.Clamp(heat, 0, 3000);
            if(Api.World.Side == EnumAppSide.Server && Api.World.BlockAccessor.GetBlock(Pos.UpCopy()).FirstCodePart() == "rock" &&heat >= 1000)
            {
                var lava = Api.World.GetBlock(new AssetLocation("game:lava-still-7"));
                Api.World.BlockAccessor.SetBlock(lava.Id,Pos.UpCopy());
            }
            LastTickTotalHours = Api.World.Calendar.TotalHours;
            MarkDirty();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            dsc.AppendLine("Accumulated Heat: " + heat);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            LastTickTotalHours = tree.GetDouble("LastTick");
            heat = tree.GetInt("heat");
            Working = tree.GetBool("working");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("LastTick", LastTickTotalHours);
            tree.SetInt("heat", heat);
            tree.SetBool("working",Fueled);
        }

    }
    public class MagmanatorBhv : BlockEntityBehavior, IManaConsumer
    {
        public MagmanatorBhv(BlockEntity blockentity) : base(blockentity)
        {
        }

        public void EatMana(int mana)
        {
            if (Blockentity is MagmanatorBe entity)
            {
                entity.Working = mana == ToVoid() && mana > 0;
            }
        }

        public int ToVoid()
        {
            if(Blockentity is MagmanatorBe)
            {
                var codepart = Api.World.BlockAccessor.GetBlock(Pos.UpCopy()).FirstCodePart();
                if (codepart == "rock" || codepart == "lava") { return 5; }
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
