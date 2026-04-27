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
    public interface BaseRuneI<T>
    {
        public string Code { get; set; }

        public string imgPath { get; set; }
        public bool Enabled { get; set; }
        public JsonObject Attributes { get; set; }
        Dictionary<string, int> Reagents { get; }
        string[] ReagNames { get; }

        public string langCode { get; set; }
        public string spellType { get; set; }
        public int spellTier { get; set; }

        public string ElementalType { get; set; }
        public bool Resolve(IWorldAccessor world, string errsrc);

        public BaseRuneI<T> Clone();


    }
}
