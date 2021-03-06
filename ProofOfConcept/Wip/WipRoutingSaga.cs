﻿using System;
using Contracts.Assemble.Commands;
using Contracts.Assemble.Events;
using Contracts.Packout.Commands;
using Contracts.Packout.Events;
using Contracts.Routing.Commands;
using Contracts.Routing.Events;
using Contracts.Scrap.Commands;
using Contracts.Scrap.Events;
using Contracts.Wip.Events;
using Domain;
using NServiceBus;
using NServiceBus.Saga;
using Persistence;

namespace Wip
{
    public class WipRoutingSaga : Saga<WipRoutingSagaData>,
                                  IAmStartedByMessages<WipReleased>,
                                  IHandleMessages<AssembleStarted>,
                                  IHandleMessages<AssemblePassed>,
                                  IHandleMessages<AssembleFailed>,
                                  IHandleMessages<WipPacked>,
                                  IHandleMessages<WipScrapped>,
                                  IHandleMessages<WipMovedToAssemble>,
                                  IHandleMessages<WipMovedToPackout>,
                                  IHandleMessages<WipMovedToScrap>,

                                  IHandleTimeouts<SimulatedAssembleDelayExpired>,
                                  IHandleTimeouts<SimulatedAssembleStartDelayExpired>,
                                  IHandleTimeouts<SimulatedPackoutDelayExpired>,
                                  IHandleTimeouts<SimulatedScrapDelayExpired>
    {
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<WipReleased>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<AssembleStarted>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<AssemblePassed>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<AssembleFailed>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<WipPacked>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<WipScrapped>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<WipMovedToAssemble>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<WipMovedToPackout>(m => m.WipId).ToSaga(s => s.WipId);
            ConfigureMapping<WipMovedToScrap>(m => m.WipId).ToSaga(s => s.WipId);
        }

        public void Handle(WipReleased message)
        {
            Data.WipId = message.WipId;

            Console.WriteLine(message.ToString());
            Bus.Send(new MoveWipToAssemble {WipId = message.WipId});
        }

        public void Handle(AssembleStarted message)
        {
            var timeout = TimeSpan.FromSeconds(new Random().Next(1, 4));
            Console.WriteLine("{0} (delay for {1} seconds)", message, timeout.TotalSeconds);

            RequestTimeout(timeout, new SimulatedAssembleDelayExpired {WipId = message.WipId});
        }

        public void Handle(AssemblePassed message)
        {
            Console.WriteLine(message.ToString());
            var wip = Repository.Get(message.WipId);
            wip.Station = "";
            Repository.Save(message.WipId, wip);
            Bus.Send(new MoveWipToPackout {WipId = message.WipId});
        }

        public void Handle(AssembleFailed message)
        {
            Console.WriteLine(message.ToString());
            var wip = Repository.Get(message.WipId);
            wip.Station = "";
            Repository.Save(message.WipId, wip);
            Bus.Send(new MoveWipToScrap {WipId = message.WipId});
        }

        public void Handle(WipPacked message)
        {
            Console.WriteLine(message.ToString());
            var wip = Repository.Get(message.WipId);
            wip.Station = "";
            wip.IsComplete = true;
            Repository.Save(message.WipId, wip);
            MarkAsComplete();
        }

        public void Handle(WipScrapped message)
        {
            Console.WriteLine(message.ToString());
            var wip = Repository.Get(message.WipId);
            wip.Station = "";
            wip.IsComplete = true;
            Repository.Save(message.WipId, wip);
            MarkAsComplete();
        }

        public void Handle(WipMovedToAssemble message)
        {
            var timeout = TimeSpan.FromSeconds(new Random().Next(1, 4));
            Console.WriteLine("{0} (delay for {1} seconds)", message, timeout.TotalSeconds);

            RequestTimeout(timeout, new SimulatedAssembleStartDelayExpired {WipId = message.WipId});
        }

        public void Handle(WipMovedToPackout message)
        {
            var timeout = TimeSpan.FromSeconds(new Random().Next(1, 4));
            Console.WriteLine("{0} (delay for {1} seconds)", message, timeout.TotalSeconds);

            RequestTimeout(timeout, new SimulatedPackoutDelayExpired {WipId = message.WipId});
        }

        public void Handle(WipMovedToScrap message)
        {
            var timeout = TimeSpan.FromSeconds(new Random().Next(1, 4));
            Console.WriteLine("{0} (delay for {1} seconds)", message, timeout.TotalSeconds);

            RequestTimeout(timeout, new SimulatedScrapDelayExpired {WipId = message.WipId});
        }

        public void Timeout(SimulatedAssembleDelayExpired state)
        {
            Console.WriteLine(state);
            var pass = DateTime.Now.Ticks%3 > 0;
            if (pass)
            {
                Console.WriteLine("{0} - (PASS)", state);
                Bus.Send(new PassAssemble {WipId = state.WipId});
            }
            else
            {
                Console.WriteLine("{0} - (FAIL)", state);
                Bus.Send(new FailAssemble {WipId = state.WipId});
            }
        }

        public void Timeout(SimulatedAssembleStartDelayExpired state)
        {
            Console.WriteLine(state);
            Bus.Send(new StartAssemble {WipId = state.WipId});
        }

        public void Timeout(SimulatedPackoutDelayExpired state)
        {
            Console.WriteLine(state);
            Bus.Send(new PackWip {WipId = state.WipId});
        }

        public void Timeout(SimulatedScrapDelayExpired state)
        {
            Console.WriteLine(state);
            Bus.Send(new ScrapWip {WipId = state.WipId});
        }
    }

    public class SimulatedAssembleDelayExpired
    {
        public Guid WipId { get; set; }

        public override string ToString()
        {
            return string.Format("Simulated assemble delay expired {0}", WipId);
        }
    }

    public class SimulatedAssembleStartDelayExpired
    {
        public Guid WipId { get; set; }

        public override string ToString()
        {
            return string.Format("Simulated assemble start delay expired {0}", WipId);
        }
    }

    public class SimulatedPackoutDelayExpired
    {
        public Guid WipId { get; set; }

        public override string ToString()
        {
            return string.Format("Simulated packout delay expired {0}", WipId);
        }
    }

    public class SimulatedScrapDelayExpired
    {
        public Guid WipId { get; set; }

        public override string ToString()
        {
            return string.Format("Simulated scrap delay expired {0}", WipId);
        }
    }
}