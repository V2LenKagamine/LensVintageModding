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
    public class BaseRefineRecipe : IByteSerializable, BaseRefineRecipeI<BaseRefineRecipe>
    {
        public string Code { get; set; }

        public bool Enabled { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes { get; set; }

        Dictionary<string, int> BaseRefineRecipeI<BaseRefineRecipe>.Reagents => Reagents;
        Dictionary<string, int> BaseRefineRecipeI<BaseRefineRecipe>.Outputs => Outputs;

        public Dictionary<string, int> Reagents;
        public Dictionary<string, int> Outputs;



        public BaseRefineRecipeI<BaseRefineRecipe> Clone() 
        {
            Dictionary<string, int> reagClone = new(Reagents.Count);
            Dictionary<string, int> outClone = new(Outputs.Count);

            for (int i = 0; i < Reagents.Count; i++) { reagClone.Add(Reagents.ElementAt(i).Key, Reagents.ElementAt(i).Value); }
            for (int i = 0; i < Outputs.Count; i++) { outClone.Add(Outputs.ElementAt(i).Key, Outputs.ElementAt(i).Value); }
            return new BaseRefineRecipe { Code = this.Code, Attributes = this.Attributes, Reagents = reagClone,Outputs = outClone};
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
                    Outputs = Attributes["outputs"].AsObject<Dictionary<string, int>>();
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
            writer.Write(Outputs.Count);
            for (int i = 0; i < Outputs.Count; i++)
            {
                writer.Write(Outputs.ElementAt(i).Key);
                writer.Write(Outputs.ElementAt(i).Value);
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
            int outsize = reader.ReadInt32();
            Outputs = new Dictionary<string, int>(outsize);
            for (int i = 0; i < outsize; i++)
            {
                Outputs.Add(reader.ReadString(), reader.ReadInt32());
            }
        }
    }
}
