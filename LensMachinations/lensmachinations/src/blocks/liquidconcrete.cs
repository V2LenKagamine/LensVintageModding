using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace LensstoryMod
{
    public class LiquidConcreteBlock : BlockForFluidsLayer,IBlockFlowing
    {
        public string Flow { get; set; }
        public Vec3i FlowNormali { get; set; }
        public bool IsLava => false;
        public int Height { get; set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Flow = Variant["flow"] is string f ? string.Intern(f) : null;
            FlowNormali = Flow != null ? Cardinal.FromInitial(Flow)?.Normali : null;
            Height = Variant["height"] is string h ? h.ToInt() : 7;
        }
        //Literally stolen from BlockWater.cs
        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            Block oldBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            if (oldBlock.DisplacesLiquids(world.BlockAccessor, blockSel.Position) && !oldBlock.IsReplacableBy(this))
            {
                failureCode = "notreplaceable";
                return false;
            }

            bool result = true;

            if (byPlayer != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                // Probably good idea to do so, so lets do it :P
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();

                failureCode = "claimed";
                return false;
            }

            bool preventDefault = false;

            foreach (BlockBehavior behavior in BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.CanPlaceBlock(world, byPlayer, blockSel, ref handled, ref failureCode);

                if (handled != EnumHandling.PassThrough)
                {
                    result &= behaviorResult;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return result;
            }

            if (preventDefault) return result;

            return true;
        }
    }
}

