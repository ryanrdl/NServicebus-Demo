﻿using Common;

namespace Messages
{
    public class OperationFailedToStartBecauseMaterialIsNotValidForRoute
    {
        public WipId WipId { get; set; }
        public MaterialId MaterialId { get; set; }
        public RouteStepId RouteStepId { get; set; }
        public ResourceId ResourceId { get; set; }
    }

    public class OperationFailedToStartBecauseWipIsAlreadyInProcess
    {
        public WipId WipId { get; set; }
        public MaterialId MaterialId { get; set; }
        public RouteStepId RouteStepId { get; set; } 
    }
}