using EpxVe.src;
using Vintagestory.API.Common;

namespace EpxVe
{
    public class EpxVeModSystem : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("BlockVeepConverter", typeof(BlockConverter));
            api.RegisterBlockEntityClass("BEVeepConverter", typeof(BEConverter));
            api.RegisterBlockEntityBehaviorClass("BEBhvVeepToEP",typeof(BEBhvConverterToEP));
            api.RegisterBlockEntityBehaviorClass("BEBhvVeepToVE", typeof(BEBhvConverterToVE));
        }
    }
}
