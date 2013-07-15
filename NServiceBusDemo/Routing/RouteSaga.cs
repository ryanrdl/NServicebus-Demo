﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Saga;

namespace Routing
{ 
    public class RoutingSaga : Saga<RouteSagaData>, 
                               IAmStartedByMessages<WipReleasedToRoute>,
         
                               IHandleMessages<WipEnqueuedAtRouteStep>, 
                               IHandleMessages<WipDequeuedAtRouteStep>,

                               IHandleMessages<OperationStartedAtRouteStep>,
                               IHandleMessages<OperationPassedAtRouteStep>,
                               IHandleMessages<OperationFailedAtRouteStep>,
                               IHandleMessages<OperationAbortedAtRouteStep>
    {

        public override void ConfigureHowToFindSaga()
        {  
            ConfigureMapping<WipEnqueuedAtRouteStep>(data => data.WipId); 
            ConfigureMapping<WipDequeuedAtRouteStep>(data => data.WipId);

            ConfigureMapping<OperationStartedAtRouteStep>(data => data.WipId);
            ConfigureMapping<OperationPassedAtRouteStep>(data => data.WipId);
            ConfigureMapping<OperationFailedAtRouteStep>(data => data.WipId);
            ConfigureMapping<OperationAbortedAtRouteStep>(data => data.WipId);
        } 

        public void Handle(WipReleasedToRoute message)
        {
            Data.RouteId = message.RouteId;
            //determine where to enqueue
            Bus.Send(new EnqueueWipAtRouteStep {WipId = message.WipId, RouteStepId = RoutingTable.FirstStep()});
        } 

        public void Handle(WipEnqueuedAtRouteStep message)
        {
            if (!Data.InQueueRouteSteps.Contains(message.RouteStepId))
            {
                Data.InQueueRouteSteps.Add(message.RouteStepId);
                //Bus.Publish(new WipEnqueuedAtRouteStep { WipId = message.WipId, RouteStepId = message.RouteStepId });
            }
            else
            {
                //Handle the case where wip was not enqueued at the requested route step so not able to dequeue it
            }
        } 

        public void Handle(WipDequeuedAtRouteStep message)
        {
            if (Data.InQueueRouteSteps.Contains(message.RouteStepId))
            {
                Data.InQueueRouteSteps.Remove(message.RouteStepId);
                //Bus.Publish(new WipDequeuedAtRouteStep { WipId = message.WipId, RouteStepId = message.RouteStepId });
            }
            else
            {
                //Handle the case where wip was not enqueued at the requested route step so not able to dequeue it
            }
        }

        public void Handle(OperationStartedAtRouteStep message)
        {
            //when an operation starts, wip should not be in queue anywhere
            foreach (var routeStepId in Data.InQueueRouteSteps)
            {
                Bus.Send(new DequeueWipAtRouteStep {WipId = message.WipId, RouteStepId = routeStepId});
            }

            Data.ChangeRouteStep(message.RouteStepId); //save resource?
        }

        public void Handle(OperationPassedAtRouteStep message)
        {
            //TODO: handle previous step was null, look into not using null check. null object pattern?
            var nextSteps = RoutingTable.NextStespOnPass(message.RouteStepId,
                                                         Data.PreviousRouteStepId.GetValueOrDefault());
            foreach (var step in nextSteps)
            {
                Bus.Send(new EnqueueWipAtRouteStep {WipId = message.WipId, RouteStepId = step});
            }
        }

        public void Handle(OperationFailedAtRouteStep message)
        {
            var nextSteps = RoutingTable.NextStepsOnFail(message.RouteStepId);

            foreach (var step in nextSteps)
            {
                Bus.Send(new EnqueueWipAtRouteStep {WipId = message.WipId, RouteStepId = step});
            }
        }

        public void Handle(OperationAbortedAtRouteStep message)
        {
            var nextSteps = RoutingTable.NextStepsOnAbort(message.RouteStepId);

            foreach (var step in nextSteps)
            {
                Bus.Send(new EnqueueWipAtRouteStep {WipId = message.WipId, RouteStepId = step});
            }
        }
    }
}