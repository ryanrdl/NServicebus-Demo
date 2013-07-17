﻿using System.Collections.Generic;
using Common;
using Domain.Identities;
using Messages.V1.Assemble;
using Messages.V1.Operations;
using NServiceBus;
using NServiceBus.Saga;

namespace Assemble
{
    //I don't think assemble is actually a saga... the only part that might be is to keep track of what has been assembled
    //and when nothing is left to be assembled, you auto complete, but that can probably just be done in the aggregate.
    public class AssembleAggregate
    {
        private AssembleState _state;

        public AssembleAggregate(AssembleState state)
        {
            _state = state;
        }


    }

    public class AssembleState
    { 
        public WipId WipId { get; set; }
        public MaterialId MaterialId { get; set; }
        public IList<MaterialId> MaterialsToAssemble { get; set; }
        public IList<MaterialId> AssembledMaterials { get; set; }
    }

    public class AssembleSaga : Saga<AssembleSagaData>,
                                IAmStartedByMessages<OperationStartedAtRouteStep>,
                                IHandleMessages<PartAssembled>,
                                IHandleMessages<PartDisassembled>,
                                IHandleMessages<CompleteAssemble>,

                                IHandleMessages<OperationAbortedAtRouteStep>,
                                IHandleMessages<OperationFailedAtRouteStep>,
                                IHandleMessages<OperationPassedAtRouteStep>
    {

        public void Handle(OperationStartedAtRouteStep message)
        {
            //loop count?
        }

        public void Handle(PartAssembled message)
        {
            var assembled = false;

            for (var i = 0; i < Data.MaterialsToAssemble.Count; i++)
            {
                if (Data.MaterialsToAssemble[i] == message.MaterialId)
                {
                    Data.AssembledMaterials.Add(Data.MaterialsToAssemble[i]);
                    Data.MaterialsToAssemble.RemoveAt(i);
                    assembled = true;
                }
            }

            if (!assembled)
            {
                //generate a fault
            }
            else if (Data.MaterialsToAssemble.Count == 0)
            {
                Bus.Send("", new CompleteAssemble {});
            }
        }

        public void Handle(PartDisassembled message)
        {
            var disassembled = false;

            for (var i = 0; i < Data.AssembledMaterials.Count; i++)
            {
                if (Data.MaterialsToAssemble[i] == message.MaterialId)
                {
                    Data.MaterialsToAssemble.Add(Data.MaterialsToAssemble[i]);
                    Data.AssembledMaterials.RemoveAt(i);
                    disassembled = true;
                }
            }

            if (!disassembled)
            {
                //generate a fault
            }
        }

        public void Handle(CompleteAssemble message)
        {
            if (Data.MaterialsToAssemble.Count > 0)
            {
                switch (Data.ResultWhenCompletingWithMaterialRemainingToBeAssembled)
                {
                    case InProcessOperationResult.Fail:
                        Bus.Publish(new OperationFailedAtRouteStep
                            {
                                WipId = message.WipId,
                                MaterialId = message.MaterialId,
                                RouteStepId = message.RouteStepId,
                                ResourceId = message.ResourceId
                            });
                        return;
                    case InProcessOperationResult.Abort:
                        Bus.Publish(new OperationAbortedAtRouteStep
                            {
                                WipId = message.WipId,
                                MaterialId = message.MaterialId,
                                RouteStepId = message.RouteStepId,
                                ResourceId = message.ResourceId
                            });
                        return;
                        //drop through to pass
                }
            }

            Bus.Publish(new OperationPassedAtRouteStep
                {
                    WipId = message.WipId,
                    MaterialId = message.MaterialId,
                    RouteStepId = message.RouteStepId,
                    ResourceId = message.ResourceId
                });
        }

        public void Handle(OperationAbortedAtRouteStep message)
        {
            //Does this really complete?  Maybe need to keep track of loop count
            MarkAsComplete();
        }

        public void Handle(OperationFailedAtRouteStep message)
        {
            //Does this really complete?  Maybe need to keep track of loop count.  Might affect first pass yield.
            MarkAsComplete();
        }

        public void Handle(OperationPassedAtRouteStep message)
        { 
            MarkAsComplete();
        }
    }
}