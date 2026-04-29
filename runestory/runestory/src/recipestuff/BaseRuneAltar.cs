using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using JsonObject = Vintagestory.API.Datastructures.JsonObject;

namespace runestory
{
    public class BaseRuneAltar : IByteSerializable, BaseRuneAltarI<BaseRuneAltar>
    {
        public string Code { get; set; }

        public bool Enabled { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes { get; set; }

        public Dictionary<string, int> Reagents;

        public Dictionary<string, int> OutputItems;
        Dictionary<string, int> BaseRuneAltarI<BaseRuneAltar>.Reagents => Reagents;
        Dictionary<string, int> BaseRuneAltarI<BaseRuneAltar>.OutputItems => OutputItems;

        public BaseRuneAltarI<BaseRuneAltar> Clone() 
        {
            Dictionary<string, int> reagClone = new(Reagents.Count);
            Dictionary<string, int> outclone = new(OutputItems.Count);

            for (int i = 0; i < Reagents.Count; i++) { reagClone.Add(Reagents.ElementAt(i).Key, Reagents.ElementAt(i).Value); }
            for (int i = 0; i < OutputItems.Count; i++) { outclone.Add(OutputItems.ElementAt(i).Key, OutputItems.ElementAt(i).Value); }
            return new BaseRuneAltar { Code = this.Code, Attributes = this.Attributes, Reagents = reagClone ,OutputItems = outclone};
        }

        public bool SatisfiesAsIngredient(int index, ItemStack inputStack)
        {
            if (Reagents.ElementAt(index).ToString().Contains('*'))
            {
                return WildcardUtil.Match(new AssetLocation(Reagents.ElementAt(index).Key), inputStack.Collectible.Code);
            }
            else
            {
                return Reagents.ElementAt(index).Key == inputStack.Collectible.Code.ToString();
            }
        }

        public bool Resolve(IWorldAccessor world,string errSrc)
        {
            if(Attributes!= null)
            {
                if (Attributes["reagents"].Exists)
                {
                    Reagents = Attributes["reagents"].AsObject<Dictionary<string, int>>();
                }
                if (Attributes["outputs"].Exists)
                {
                    OutputItems = Attributes["outputs"].AsObject<Dictionary<string, int>>();
                }
            }
            return true;
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Code != null);
            if (Code != null) { writer.Write(Code); }
            writer.Write(Attributes != null);
            if (Attributes != null)
            {
                writer.Write(Attributes.Token.ToString());
            }
            writer.Write(Reagents.Count);
            for (int i = 0; i < Reagents.Count; i++)
            {
                writer.Write(Reagents.ElementAt(i).Key);
                writer.Write(Reagents.ElementAt(i).Value);
            }
            writer.Write(OutputItems.Count);
            for (int i = 0; i < OutputItems.Count; i++)
            {
                writer.Write(OutputItems.ElementAt(i).Key);
                writer.Write(OutputItems.ElementAt(i).Value);
            }
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadBoolean() ? reader.ReadString() : null;
            Attributes = reader.ReadBoolean() ? new JsonObject(JToken.Parse(reader.ReadString())) : null;
            int reagsize = reader.ReadInt32();
            Reagents = new Dictionary<string, int>(reagsize);
            for (int i = 0; i < reagsize; i++)
            {
                Reagents.Add(reader.ReadString(),reader.ReadInt32());
            }
            int namesize = reader.ReadInt32();
            OutputItems = new Dictionary<string, int>(namesize);
            for (int i = 0; i < namesize; i++)
            {
                OutputItems.Add(reader.ReadString(), reader.ReadInt32());
            }
        }
    }
}
