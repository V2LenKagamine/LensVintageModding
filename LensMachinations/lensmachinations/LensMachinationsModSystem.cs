using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace LensstoryMod
{
    public class LensMachinationsMod : ModSystem
    {
        private readonly HashSet<KeyValuePair<ManaNetwork, int>> mananetworks = new();
        private readonly List<ManaConsumer> consumers = new();
        private readonly Dictionary<BlockPos, ManaPart> ManaParts = new();


        public IServerNetworkChannel servermanachannel;
        public IClientNetworkChannel clientmanachannel;
        public IServerNetworkChannel serverRedChannel;
        public IClientNetworkChannel clientRedChannel;
        public IServerNetworkChannel serverRecipeChannel;
        public IClientNetworkChannel clientRecipeChannel;
        ICoreClientAPI storedcapi;
        ICoreServerAPI storedsapi;
        public AttunementWandGui manathing;
        public RedWandGui redthing;

        public static ILogger logger;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("attunementclass", typeof(AttunementWandItem));
            api.RegisterItemClass("redclass", typeof(RedWandItem));

            api.RegisterBlockClass("frameblock", typeof(WoodFrame));
            api.RegisterBlockClass("liquidconcreteblock", typeof(LiquidConcreteBlock));
            api.RegisterBlockEntityClass("lenssimpletoggle", typeof(SimpleToggleBlockBe));
            api.RegisterBlockEntityBehaviorClass("lenssimpletogglebehavior", typeof(SimpleToggleBlockBhv));

            RegisterTrio(api, "swapper", typeof(SwapperBlock), typeof(SwapperBE), typeof(SwapperBhv));

            RegisterTrio(api, "creativemana", typeof(CreativeMana), typeof(CreativeManaBE), typeof(CreativeManaBhv));

            RegisterTrio(api, "furnacegen", typeof(Burnerator), typeof(BurneratorBE), typeof(BurneratorBhv));

            RegisterTrio(api, "autopanner", typeof(AutoPannerBlock), typeof(AutoPannerBE), typeof(AutoPannerBhv));

            RegisterTrio(api, "manarepair", typeof(ManaRepairBlock), typeof(ManaRepairBE), typeof(ManaRepairBhv));

            RegisterTrio(api, "rockmaker", typeof(RockmakerBlock), typeof(RockmakerBE), typeof(RockmakerBhv));

            RegisterTrio(api, "lever", typeof(LeverBlock), typeof(LeverBE), typeof(LeverBhv));

            RegisterTrio(api, "kineticmpgen", typeof(KineticpotentianatorBlock), typeof(KineticpotentianatorBe), typeof(KineticpotentianatorBhv));

            RegisterTrio(api, "icebox", typeof(RefridgerationUnitBlock), typeof(RefridgerationUnitBE), typeof(RefridgerationUnitBhv));

            api.RegisterBlockEntityClass("lensmagmanator", typeof(MagmanatorBe));
            api.RegisterBlockEntityBehaviorClass("lensmagmanatorbehavior", typeof(MagmanatorBhv));

            api.RegisterBlockClass("lensredtransmission", typeof(RedstoneTransmissionBlock));
            api.RegisterBlockEntityBehaviorClass("lensredtransmissionbehavior", typeof(RedstoneTransmissionBhv));

            api.RegisterBlockEntityBehaviorClass("Mana", typeof(Mana));

            api.RegisterBlockEntityBehaviorClass("Redstone", typeof(Redstone));

            api.Event.RegisterGameTickListener(OnGameTick, 1000);

            api.Event.RegisterGameTickListener(OnRedTick, 250);

            logger = api.Logger;
        }

        private static void RegisterTrio(ICoreAPI api, string basename, Type block, Type blockEntity, Type entBehavior)
        {
            api.RegisterBlockClass("lens" + basename + "block", block);
            api.RegisterBlockEntityClass("lens" + basename, blockEntity);
            api.RegisterBlockEntityBehaviorClass("lens" + basename + "behavior", entBehavior);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            storedsapi = api;

            servermanachannel = api.Network.RegisterChannel("manamessages")
                .RegisterMessageType(typeof(ManaWandMessage))
                .RegisterMessageType(typeof(ManaWandResponce))
                .SetMessageHandler<ManaWandMessage>(OnManaMessageS);

            serverRedChannel = api.Network.RegisterChannel("redmessages")
                .RegisterMessageType(typeof(RedWandMessage))
                .SetMessageHandler<RedWandMessage>(OnRedMessageRec);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            storedcapi = api;

            clientmanachannel = api.Network.RegisterChannel("manamessages")
                .RegisterMessageType(typeof(ManaWandMessage))
                .RegisterMessageType(typeof(ManaWandResponce))
                .SetMessageHandler<ManaWandResponce>(OnManaMessageC);

            clientRedChannel = api.Network.RegisterChannel("redmessages")
                .RegisterMessageType(typeof(RedWandMessage));

            manathing = new AttunementWandGui(api);
            redthing = new RedWandGui(api);
        }

        internal static void LogError(string message)
        {
            logger?.Error("(LensStory):{0}", message);
        }
        #region NetworkingBullshit

        private void OnManaMessageS(IPlayer from, ManaWandMessage packet)
        {
            if (from.InventoryManager.ActiveHotbarSlot.Itemstack?.Item?.Code == AssetLocation.Create("lensmachinations:attunementwand"))
            {
                from.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetInt("channel", packet.message);
                from.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
        }

        private void OnManaMessageC(ManaWandResponce packet)
        {
            storedcapi.ShowChatMessage("You attune the wand to channel " + packet.message);
        }
        private void OnRedMessageRec(IPlayer from, RedWandMessage up)
        {
            if (from.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code == AssetLocation.Create("lensmachinations:redwand"))
            {
                from.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetString("channel", up.Channel);
                from.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ManaWandMessage
        {
            public int message;
        }
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class ManaWandResponce
        {
            public int message;
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class RedWandMessage
        {
            public string Channel;
        }
        #endregion

        //Mana stuff start
        #region Manastuff
        public bool DoUpdate(BlockPos pos, int code, bool forced = false)
        {
            if (!this.ManaParts.TryGetValue(pos, out var part))
            {
                if (code == 0) return false;
                part = this.ManaParts[pos] = new ManaPart(pos);
            }
            if (code == part.ManaCode && !forced) { return false; }

            part.ManaCode = code;

            this.AddManaConnection(ref part, code);

            if (part.ManaCode == 0)
            {
                this.ManaParts.Remove(pos);
            }

            return true;
        }
        private void OnGameTick(float _)
        {
            foreach (var network in mananetworks)
            {
                consumers.Clear();

                var totalMana = network.Key.Makers.Sum(maker => maker.MakeMana());

                var neededMana = 0;

                foreach (var consumer in network.Key.Consumers.Select(theconsumer => new ManaConsumer(theconsumer)))
                {
                    neededMana += consumer.ManaNeeded;
                    consumers.Add(consumer);
                }

                do
                {
                    foreach (var customer in this.consumers)
                    {
                        if (neededMana > totalMana)
                        {
                            customer.ManaEater.EatMana(0);
                        }
                        else
                        {
                            customer.ManaEater.EatMana(customer.ManaNeeded);
                            neededMana -= customer.ManaNeeded;
                        }

                    }
                }
                while (totalMana >= neededMana && neededMana > 0);

            }
        }

        public class ManaNetwork
        {
            public readonly HashSet<IManaMaker> Makers = new();
            public readonly HashSet<IManaConsumer> Consumers = new();
            public readonly HashSet<BlockPos> Positions = new();

            public int ManaID;
            public int Consumed { get => this.Consumers.Sum(consumer => consumer.ToVoid()); }
            public int Produced { get => this.Makers.Sum(producer => producer.MakeMana()); }

            public ManaNetwork(int ManaID)
            {
                this.ManaID = ManaID;
            }

        }
        public ManaNetwork CreateManaNetwork(int ManaID)
        {
            var manaboi = new ManaNetwork(ManaID);
            var compressedboi = new KeyValuePair<ManaNetwork, int>(manaboi, ManaID);
            this.mananetworks.Add(compressedboi);
            return manaboi;
        }
        public void Remove(BlockPos pos)
        {
            if (ManaParts.TryGetValue(pos, out var part))
            {
                ManaParts.Remove(pos);
                RemoveManaConnection(ref part, part.ManaCode);
            }
        }
        private void RemoveManaConnection(ref ManaPart part, int manaID)
        {
            var targets = mananetworks.Where(net => net.Value == manaID);
            if (targets.Any())
            {
                for (int i = 0; i < targets.Count(); i++)
                {
                    ManaNetwork target = targets.ElementAt(i).Key;
                    target.Positions.Remove(part.Position);
                    if (part.Consumer is { })
                    {
                        target.Consumers.Remove(part.Consumer);
                    }
                    if (part.Maker is { })
                    {
                        target.Makers.Remove(part.Maker);
                    }
                    part.ManaNet = null;
                }
            }
        }

        public void AddManaConnection(ref ManaPart part, int manaID)
        {
            if (manaID == 0)
            {
                return;
            }
            if (mananetworks.Count >= 1)
            {
                if (mananetworks.Where(output => output.Value == manaID).Count() >= 1)
                {
                    part.ManaNet = mananetworks.Where(output => output.Value == manaID).First().Key;
                }
                else
                {
                    part.ManaNet = CreateManaNetwork(manaID);
                }
            }
            else
            {
                part.ManaNet = CreateManaNetwork(manaID);
            }
        }


        public void SetManaConsumer(BlockPos pos, IManaConsumer? eater)
        {
            if (!this.ManaParts.TryGetValue(pos, out var part))
            {
                if (eater == null)
                {
                    return;
                }
                part = this.ManaParts[pos] = new ManaPart(pos);
            }
            if (part.Consumer != eater)
            {
                if (part.Consumer is not null)
                {
                    part.ManaNet?.Consumers.Remove(part.Consumer);
                }
            }

            if (eater is not null)
            {
                part.ManaNet?.Consumers.Add(eater);
            }
            part.Consumer = eater;
        }
        public void SetManaProducer(BlockPos pos, IManaMaker? maker)
        {
            if (!this.ManaParts.TryGetValue(pos, out var part))
            {
                if (maker == null)
                {
                    return;
                }
                part = this.ManaParts[pos] = new ManaPart(pos);
            }
            if (part.Maker != maker)
            {
                if (part.Maker is not null)
                {
                    part.ManaNet?.Makers.Remove(part.Maker);
                }
            }
            if (maker is not null)
            {
                part.ManaNet?.Makers.Add(maker);
            }
            part.Maker = maker;
        }

        public ManaNetInfo GetManaNetInfo(BlockPos pos)
        {
            var result = new ManaNetInfo();

            if (this.ManaParts.TryGetValue(pos, out var part))
            {
                if (part.ManaNet is { } net)
                {
                    result.TotalBlocks = net.Positions.Count;
                    result.TotalMakers = net.Makers.Count;
                    result.TotalConsumers = net.Consumers.Count;
                    result.ManaProduced = net.Produced;
                    result.ManaConsumned = net.Consumed;
                    result.ManaID = net.ManaID;
                }
            }
            return result;
        }

        public class ManaPart
        {
            public ManaNetwork? ManaNet = null;
            public readonly BlockPos Position;
            public int ManaCode = 0;
            public IManaConsumer? Consumer;
            public IManaMaker? Maker;

            public ManaPart(BlockPos pos)
            {
                this.Position = pos;
            }
        }

        public class ManaNetInfo
        {
            public int ManaConsumned;
            public int TotalBlocks;
            public int TotalConsumers;
            public int TotalMakers;
            public int ManaProduced;
            public int ManaID;
        }

        #endregion

        //Yes this is copied from the mana but dumbed down deal with it.
        #region RedstoneManaCopy


        private readonly HashSet<KeyValuePair<RedNetwork, string>> rednetworks = new();
        private readonly List<RedstoneTaker> receivers = new();
        private readonly Dictionary<BlockPos, Redpart> RedParts = new();
        public bool DoRedUpdate(BlockPos pos, string code, bool forced = false)
        {
            if (!RedParts.TryGetValue(pos, out var part))
            {
                if (code == null) return false;
                part = RedParts[pos] = new Redpart(pos);
            }
            if (code == part.Network && !forced) { return false; }

            part.Network = code;
            
            this.AddRedConnection(ref part, code);

            if (part.Network == null)
            {
                this.RedParts.Remove(pos);
            }

            return true;
        }
        public bool DoOutRedUpdate(BlockPos pos, string code, bool forced = false)
        {

            if (!RedParts.TryGetValue(pos, out var part))
            {
                if (code == null) return false;
                part = RedParts[pos] = new Redpart(pos);
            }
            if (code == part.OutNetwork && !forced) { return false; }

            part.OutNetwork = code;

            this.AddRedConnection(ref part, code);

            if (part.OutNetwork == null)
            {
                this.RedParts.Remove(pos);
            }

            return true;
        }
        private void OnRedTick(float _)
        {
            foreach (var network in rednetworks)
            {
                receivers.Clear();

                if (network.Value == null)
                {
                    rednetworks.Remove(network);
                    continue;
                }

                bool Signaled = false;

                foreach (var sender in network.Key.Makers)
                {
                    if (sender.Active())
                    {
                        Signaled = true; break;
                    }
                }
                foreach (var consumer in network.Key.Consumers.Select(theconsumer => new RedstoneTaker(theconsumer)))
                {
                    receivers.Add(consumer);
                }
                foreach (var customer in receivers)
                {
                    customer.Taker.OnSignal(Signaled);
                }
            }
        }

        public class RedNetwork
        {
            public readonly HashSet<IRedstoneSender> Makers = new();
            public readonly HashSet<IRedstoneTaker> Consumers = new();
            public readonly HashSet<BlockPos> Positions = new();

            public string Network;

            public RedNetwork(string ManaID)
            {
                this.Network = ManaID;
            }

        }
        public RedNetwork CreateRedNetwork(string ManaID)
        {
            var manaboi = new RedNetwork(ManaID);
            var compressedboi = new KeyValuePair<RedNetwork, string>(manaboi, ManaID);
            this.rednetworks.Add(compressedboi);
            return manaboi;
        }
        public void RedRemove(BlockPos pos)
        {
            if (this.RedParts.TryGetValue(pos, out var part))
            {
                RemoveRedConnection(ref part, part.Network, part.OutNetwork);
                RedParts.Remove(pos);
            }
        }
        private void RemoveRedConnection(ref Redpart part, string networkID, string outNetworkID)
        {
            var targets = rednetworks.Where(net => net.Value == networkID || net.Value == outNetworkID);
            if (targets.Any())
            {
                for (int i = 0; i < targets.Count(); i++)
                {
                    RedNetwork target = targets.ElementAt(i).Key;
                    target.Positions.Remove(part.Position);
                    if (part.Consumer is { })
                    {
                        target.Consumers.Remove(part.Consumer);
                    }
                    if (part.Maker is { })
                    {
                        target.Makers.Remove(part.Maker);
                    }
                    part.Network = null;
                    part.OutNetwork = null;
                }
            }
        }

        public void AddRedConnection(ref Redpart part, string manaID)
        {
            if (manaID == "")
            {
                return;
            }
            if (rednetworks.Count >= 1)
            {
                if (rednetworks.Where(output => output.Value == manaID).Count() >= 1)
                {
                    part.RedNet = rednetworks.Where(output => output.Value == manaID).First().Key;
                }
                else
                {
                    part.RedNet = CreateRedNetwork(manaID);
                }
            }
            else
            {
                part.RedNet = CreateRedNetwork(manaID);
            }
        }


        public void SetRedTaker(BlockPos pos, IRedstoneTaker eater)
        {
            if (!this.RedParts.TryGetValue(pos, out var part))
            {
                if (eater == null)
                {
                    return;
                }
                part = this.RedParts[pos] = new Redpart(pos);
            }
            if (part.Consumer != eater)
            {
                if (part.Consumer is not null)
                {
                    part.RedNet?.Consumers.Remove(part.Consumer);
                }
            }

            if (eater is not null)
            {
                part.RedNet?.Consumers.Add(eater);
            }
            part.Consumer = eater;
        }
        public void SetRedProducer(BlockPos pos, IRedstoneSender? maker)
        {
            if (!this.RedParts.TryGetValue(pos, out var part))
            {
                if (maker == null)
                {
                    return;
                }
                part = this.RedParts[pos] = new Redpart(pos);
            }
            if (part.Maker != maker)
            {
                if (part.Maker is not null)
                {
                    part.RedNet?.Makers.Remove(part.Maker);
                }
            }
            if (maker is not null)
            {
                part.RedNet?.Makers.Add(maker);
            }
            part.Maker = maker;
        }

        public RedNetInfo GetRedNetInfo(BlockPos pos)
        {
            var result = new RedNetInfo();

            if (this.RedParts.TryGetValue(pos, out var part))
            {
                if (part.RedNet is { } net)
                {
                    result.TotalBlocks = net.Positions.Count;
                    result.TotalMakers = net.Makers.Count;
                    result.TotalConsumers = net.Consumers.Count;
                    result.NetworkID = part.Network;
                    result.OutNetworkID = part.OutNetwork;
                }
            }
            return result;
        }

        public class Redpart
        {
            public RedNetwork? RedNet = null;
            public readonly BlockPos Position;
            public string Network = "";
            public string OutNetwork = "";
            public IRedstoneTaker? Consumer;
            public IRedstoneSender? Maker;

            public Redpart(BlockPos pos)
            {
                this.Position = pos;
            }
        }

        public class RedNetInfo
        {
            public int TotalBlocks;
            public int TotalConsumers;
            public int TotalMakers;
            public string NetworkID;
            public string OutNetworkID;
        }
        #endregion
    }
}
