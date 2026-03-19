using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HierarchicalConsolidation.Models
{
    /// <summary>
    /// Represents a node in the hierarchical structure
    /// </summary>
    public class HierarchicalNode
    {
        [Key]
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public string ParentId { get; set; }
        
        public bool IsConsolidated { get; set; }
        
        public ConsolidationType ConsolidationType { get; set; }
        
        public DateTime? ConsolidatedDate { get; set; }
        
        public string ConsolidatedBy { get; set; }
        
        // Navigation properties
        public virtual HierarchicalNode Parent { get; set; }
        
        public virtual ICollection<HierarchicalNode> Children { get; set; } = new List<HierarchicalNode>();
        
        // Additional properties for audit trail
        public DateTime CreatedDate { get; set; }
        
        public DateTime ModifiedDate { get; set; }
        
        public string CreatedBy { get; set; }
        
        public string ModifiedBy { get; set; }
    }
    
    public enum ConsolidationType
    {
        None = 0,
        Manual = 1,
        Automatic = 2,
        Inherited = 3
    }
}