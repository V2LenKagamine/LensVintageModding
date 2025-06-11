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
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace EpxVe.src
{

    public class BEConverter : BlockEntity
    {
        public BEConverter()
        {
        }

        protected BEBehaviorElectricalProgressive EPSys => GetBehavior<BEBehaviorElectricalProgressive>();


        public Facing epfacing;
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (EPSys == null)
                return;

            Facing correct = Facing.AllAll;
            switch(Block.FirstCodePart(2))
            {
                case "down":
                    {
                        correct = Facing.DownAll;
                        break;
                    }
                case "up":
                    {
                        correct = Facing.UpAll;
                        break;
                    }
                case "north":
                    {
                        correct = Facing.NorthAll;
                        break;
                    }
                case "east":
                    {
                        correct = Facing.EastAll;
                        break;
                    }
                case "south":
                    {
                        correct = Facing.SouthAll;
                        break;
                    }
                case "west":
                    {
                        correct = Facing.WestAll;
                        break;
                    }
            }
            EParams thing = new(32, 5f, "", 0, 1, 1, false, false, true);
            EPSys!.Connection = correct;
            EPSys.Eparams = ( thing,FacingHelper.Faces(correct).First().Index);
        }
    }
}
