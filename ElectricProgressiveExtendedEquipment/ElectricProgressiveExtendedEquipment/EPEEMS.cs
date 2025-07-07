using ElectricProgressiveExtendedEquipment.src.items;
using Vintagestory.API.Common;

namespace ElectricProgressiveExtendedEquipment
{
    public class EPEEMS : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("EPEElectricBow", typeof(ElecBow));
            api.RegisterItemClass("EPEElectricPalette", typeof(ElectricPalette));
        }
    }
}
