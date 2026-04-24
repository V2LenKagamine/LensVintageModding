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
    public class BaseRuneSpell : IByteSerializable, BaseRuneI<BaseRuneSpell>
    {
        public string Code { get; set; }

        public bool Enabled { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes { get; set; }

        public Dictionary<string, int> Reagents;

        public string[] ReagNames;

        public string ElementalType { get; set; }
        Dictionary<string, int> BaseRuneI<BaseRuneSpell>.Reagents => Reagents;
        string[] BaseRuneI<BaseRuneSpell>.ReagNames => ReagNames;
        public string langCode { get; set; }
        public string imgPath { get; set; }

        public BaseRuneI<BaseRuneSpell> Clone() 
        {
            Dictionary<string, int> reagClone = new(Reagents.Count);
            string[] namesclone = new string[ReagNames.Length];

            for (int i = 0; i < Reagents.Count; i++) { reagClone.Add(Reagents.ElementAt(i).Key, Reagents.ElementAt(i).Value); }
            for (int i = 0; i < ReagNames.Length; i++) { namesclone[i] = ReagNames[i]; }
            return new BaseRuneSpell { Code = this.Code, Attributes = this.Attributes, Reagents = reagClone ,ReagNames = namesclone,langCode= langCode,ElementalType = ElementalType ?? "none"};
        }

        public bool SatisfiesAsIngredient(int index, ItemStack inputStack)
        {
            return WildcardUtil.Match(new AssetLocation(Reagents.ElementAt(index).Key),inputStack.Collectible.Code);
        }

        public bool Resolve(IWorldAccessor world,string errSrc)
        {
            if(Attributes!= null)
            {
                if (Attributes["reagents"].Exists)
                {
                    Reagents = Attributes["reagents"].AsObject<Dictionary<string, int>>();
                }
                if (Attributes["reagentnames"].Exists)
                {
                    ReagNames = Attributes["reagentnames"].AsObject<string[]>();
                }
                if(Attributes["elementType"].Exists){
                    ElementalType = Attributes["elementType"].AsObject<string>();
                }
                else
                {
                    ElementalType = "none";
                }
                if (Attributes["langCode"].Exists)
                {
                    langCode = Attributes["langCode"].AsObject<string>();
                }
                else
                {
                    langCode = "nodsc";
                }
            }
            return true;
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Code != null);
            if (Code != null) { writer.Write(Code); }
            writer.Write(imgPath ?? "bad");
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
            writer.Write(ReagNames.Length);
            for (int i = 0; i < ReagNames.Length; i++)
            {
                writer.Write(ReagNames[i]);
            }
            writer.Write(langCode ?? "nodsc");
            writer.Write(ElementalType ?? "none");
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadBoolean() ? reader.ReadString() : null;
            imgPath = reader.ReadString();
            Attributes = reader.ReadBoolean() ? new JsonObject(JToken.Parse(reader.ReadString())) : null;
            int reagsize = reader.ReadInt32();
            Reagents = new Dictionary<string, int>(reagsize);
            for (int i = 0; i < reagsize; i++)
            {
                Reagents.Add(reader.ReadString(),reader.ReadInt32());
            }
            int namesize = reader.ReadInt32();
            ReagNames = new string[namesize];
            for (int i = 0; i < namesize; i++)
            {
                ReagNames[i] = reader.ReadString();
            }
            langCode = reader.ReadString();
            ElementalType = reader.ReadString();
        }
    }
}
