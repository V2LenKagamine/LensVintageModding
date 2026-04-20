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
        public string[] Reagents { get; }

        public BaseRuneI<T> Clone();


    }
}
