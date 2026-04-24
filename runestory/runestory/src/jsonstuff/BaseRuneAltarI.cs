using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace runestory
{
    public interface BaseRuneAltarI<T>
    {
        public string Code { get; set; }
        public bool Enabled { get; set; }
        public JsonObject Attributes { get; set; }
        Dictionary<string, int> Reagents { get; }
        Dictionary<string, int> OutputItems { get; }


        public bool Resolve(IWorldAccessor world, string errsrc);

        public BaseRuneAltarI<T> Clone();


    }
}
