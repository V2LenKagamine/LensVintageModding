using Vintagestory.API.Common;

namespace LensGemology
{
    class Init: ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            CrystalColour.InitColours();
            CrystalColour.InitLights();
        }
        public override void Dispose()
        {
            base.Dispose();

            CrystalColour.Destroy();
        }
    }
}
