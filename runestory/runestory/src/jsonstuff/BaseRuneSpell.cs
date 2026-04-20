using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
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
        public string[] Reagents;
        string[] BaseRuneI<BaseRuneSpell>.Reagents { get { return Reagents; } }

        public string imgPath { get; set; }

        public BaseRuneI<BaseRuneSpell> Clone()
        {
            string[] reagClone = new string[Reagents.Length];

            for (int i = 0; i < Reagents.Length; i++) { reagClone[i] = Reagents[i]; }
            return new BaseRuneSpell { Code = this.Code, Attributes = this.Attributes, Reagents = reagClone };
        }

        public bool SatisfiesAsIngredient(int index, ItemStack inputStack)
        {
            return WildcardUtil.Match(new AssetLocation(Reagents[index]),inputStack.Collectible.Code);
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
            writer.Write(Reagents.Length);
            for (int i = 0; i < Reagents.Length; i++)
            {
                writer.Write(Reagents[i]);
            }
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadBoolean() ? reader.ReadString() : null;
            imgPath = reader.ReadString();
            Attributes = reader.ReadBoolean() ? new JsonObject(JToken.Parse(reader.ReadString())) : null;
            Reagents = new string[reader.ReadInt32()];
            for (int i = 0; i < Reagents.Length; i++)
            {
                Reagents[i] = reader.ReadString();
            }
        }
    }
}
