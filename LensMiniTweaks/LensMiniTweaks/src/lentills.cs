using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace LensMiniTweaksModSystem
{
    public static class Lentills
    {
        public static string FindInArray(string needle, string[] haystack)
        {
            if (needle == null || haystack == null || haystack.Length <= 0)
                return "";

            foreach (string hay in haystack)
            {
                if (hay == needle || WildcardUtil.Match(hay, needle))
                    return hay;
            }

            return "";
        }

        public static bool IsChunkAreaLoaded(BlockPos pos,IBlockAccessor blockAccessor, int range)
        {
            var chunksize = GlobalConstants.ChunkSize;
            var mincx = (pos.X - range) / chunksize;
            var maxcx = (pos.X + range) / chunksize;
            var mincz = (pos.Z - range) / chunksize;
            var maxcz = (pos.Z + range) / chunksize;

            for (var cx = mincx; cx <= maxcx; cx++)
            {
                for (var cz = mincz; cz <= maxcz; cz++)
                {
                    if (blockAccessor.GetChunk(cx, pos.Y / chunksize, cz) == null)
                    { return false; }
                }
            }
            return true;
        }


    }
}
