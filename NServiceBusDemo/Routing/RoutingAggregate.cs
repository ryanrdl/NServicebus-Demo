﻿using System.Collections.Generic;
using Domain.Identities;
using Infrastructure;
using Messages.V1.Operations;

namespace Routing
{
    public class RoutingAggregate
    {
        private RoutingState _state;
        public RoutingAggregate(RoutingState state)
        {
            _state = state;
        }

        public void Handle(StartWipAtOperation command)
        {
            var inProcessRouteStep = _state.GetCurrentInProcessRouteStepForWip(command.WipId);

            if (!inProcessRouteStep.IsEmpty())
            {
                Faults.Raise(new OperationFailedToStartBecauseWipIsAlreadyInProcess
                    {
                        WipId = command.WipId,
                        RouteStepId = inProcessRouteStep,
                        MaterialId = command.MaterialId
                    });
            }
            else if (!_state.CheckMaterialValidForRoute(command.MaterialId))
            {
                Faults.Raise(new OperationFailedToStartBecauseMaterialIsNotValidForRoute
                    {
                        WipId = command.WipId,
                        RouteStepId = command.RouteStepId,
                        ResourceId = command.ResourceId,
                        MaterialId = command.MaterialId
                    });
            }
            else
            {
                DomainEvents.Publish(new OperationStartedAtRouteStep
                    {
                        WipId = command.WipId,
                        RouteStepId = command.RouteStepId,
                        ResourceId = command.ResourceId,
                        MaterialId = command.MaterialId
                    });
            }
        }
    }

    public class RoutingState
    {
        public RouteId RouteId { get; set; }
        public IList<MaterialId> AssociatedMaterialIds { get; set; }
        public IDictionary<WipId, RouteStepId> InProcessWip { get; set; } 
        
        public bool CheckMaterialValidForRoute(MaterialId materialId)
        {
            return AssociatedMaterialIds.Contains(materialId);
        }

        public RouteStepId GetCurrentInProcessRouteStepForWip(WipId wipId)
        {
            return InProcessWip.ContainsKey(wipId) ? InProcessWip[wipId] : new RouteStepId();
        }

        public RoutingState()
        {
            AssociatedMaterialIds = new List<MaterialId>();
            InProcessWip = new Dictionary<WipId, RouteStepId>();
        }
    }  


}