using System;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Audit.Types
{
    public class AuditMessage
    {
        public Entity AffectedEntity { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public Source Source { get; set; }
        public List<PropertyUpdate> ChangedProperties { get; set; }
        public DateTime ChangeAt { get; set; }
        public Actor ChangedBy { get; set; }
        public List<Entity> RelatedEntities { get; set; }
    }
}
