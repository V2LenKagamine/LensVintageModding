using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TempMach
{
    public class TempMachModSystem : ModSystem
    {

        public IServerNetworkChannel serverRedChannel;
        public IClientNetworkChannel clientRedChannel;
        ICoreClientAPI storedcapi;
        ICoreServerAPI storedsapi;
        public RedWandGui redthing;
        private long redlistener;
        public static ILogger logger;
        /*Register Utils*/
        private static void RegisterTrio(ICoreAPI api, string basename, Type block, Type blockEntity, Type entBehavior)
        {
            api.RegisterBlockClass("tempmach_" + basename + "_b", block);
            api.RegisterBlockEntityClass("tempmach_" + basename + "_be", blockEntity);
            api.RegisterBlockEntityBehaviorClass("tempmach_" + basename + "_bhv", entBehavior);
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("machwanditem", typeof(RedWandItem));

            RegisterTrio(api, "lever", typeof(LeverBlock), typeof(LeverBE), typeof(LeverBhv));
            RegisterTrio(api, "swapper", typeof(SwapperBlock), typeof(SwapperBE), typeof(SwapperBhv));
            RegisterTrio(api,"transmission",typeof(RedstoneTransmissionBlock),typeof(RedstoneTransmissionBE), typeof(RedstoneTransmissionBhv));
            RegisterTrio(api, "piston", typeof(PistonBlock), typeof(PistonBE), typeof(PistonBhv));
            api.RegisterBlockClass("temppistonhead", typeof(PistonHead));
            api.RegisterBlockEntityClass("temppistheadbe",typeof(PistonHeadBE));


            api.RegisterBlockEntityClass("tempmach_simpletoggle_be", typeof(SimpleToggleBlockBe));
            api.RegisterBlockEntityBehaviorClass("tempmach_simpletoggle_bhv", typeof(SimpleToggleBlockBhv));

            api.RegisterBlockBehaviorClass("LenBlockCoverWithDirection", typeof(BlockBehaviorCoverWithDirection));
            api.RegisterBlockEntityBehaviorClass("TempNet", typeof(Redstone));

            redlistener = api.Event.RegisterGameTickListener(OnRedTick, 250);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);

            storedsapi = api;

            serverRedChannel = api.Network.RegisterChannel("redmessages")
                .RegisterMessageType(typeof(RedWandMessage))
                .SetMessageHandler<RedWandMessage>(OnRedMessageRec);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            storedcapi = api;

            clientRedChannel = api.Network.RegisterChannel("redmessages")
                .RegisterMessageType(typeof(RedWandMessage));

            redthing = new RedWandGui(api);
        }
        /*
        // Maybe needed? Maybe not? Idk
        public override void Dispose()
        {
            base.Dispose();

            redthing = null;
            try
            {
                storedsapi.Event.UnregisterGameTickListener(redlistener);
            }
            catch(Exception e)
            {
                logger.LogException(EnumLogType.Error,e);
            }
            redlistener = 0;

            storedcapi = null;

            storedsapi = null;
        }
        */


        /*Rednet stuff*/
        private void OnRedMessageRec(IPlayer from, RedWandMessage up)
        {
            if (from.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code == AssetLocation.Create("tempmach:machwand"))
            {
                from.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetString("channel", up.Channel);
                from.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class RedWandMessage
        {
            public string Channel;
        }


        #region The Bulk
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

            AddRedConnection(ref part, code);

            if (part.OutNetwork == null)
            {
                RedParts.Remove(pos);
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
                for (int i = 0; i < network.Key.Consumers.Count;i++)
                {
                    BlockEntityBehavior? boi = (network.Key.Consumers.ElementAt(i) as BlockEntityBehavior);
                    var consumer = new RedstoneTaker(network.Key.Consumers.ElementAt(i));

                    if (boi?.Blockentity != null && boi.Blockentity.GetBehavior<Redstone>().Frequency == network.Value)
                    {
                        receivers.Add(consumer);
                    }
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
                Network = ManaID;
            }

        }
        public RedNetwork CreateRedNetwork(string ManaID)
        {
            var manaboi = new RedNetwork(ManaID);
            var compressedboi = new KeyValuePair<RedNetwork, string>(manaboi, ManaID);
            rednetworks.Add(compressedboi);
            return manaboi;
        }
        public void RedRemove(BlockPos pos)
        {
            if (RedParts.TryGetValue(pos, out var part))
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
            if (!RedParts.TryGetValue(pos, out var part))
            {
                if (eater == null)
                {
                    return;
                }
                part = RedParts[pos] = new Redpart(pos);
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
            if (!RedParts.TryGetValue(pos, out var part))
            {
                if (maker == null)
                {
                    return;
                }
                part = RedParts[pos] = new Redpart(pos);
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

            if (RedParts.TryGetValue(pos, out var part))
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
                Position = pos;
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
