using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HierarchicalConsolidation.Models;

namespace HierarchicalConsolidation.Services
{
    /// <summary>
    /// Interface for hierarchical consolidation operations
    /// </summary>
    public interface IHierarchicalConsolidationService
    {
        /// <summary>
        /// Consolidates a node and all its ancestors in a bulk operation
        /// </summary>
        /// <param name="nodeId">The ID of the node to consolidate</param>
        /// <param name="consolidationType">Type of consolidation</param>
        /// <param name="userId">User performing the consolidation</param>
        /// <returns>List of consolidated nodes</returns>
        Task<ConsolidationResult> ConsolidateNodeWithAncestorsAsync(string nodeId, ConsolidationType consolidationType, string userId);
        
        /// <summary>
        /// Gets all ancestors of a given node
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>List of ancestor nodes</returns>
        Task<List<HierarchicalNode>> GetAncestorsAsync(string nodeId);
        
        /// <summary>
        /// Gets the complete hierarchy path from root to the specified node
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>Ordered list from root to node</returns>
        Task<List<HierarchicalNode>> GetHierarchyPathAsync(string nodeId);
        
        /// <summary>
        /// Validates if consolidation is allowed for the given node
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateConsolidationAsync(string nodeId);
        
        /// <summary>
        /// Performs bulk consolidation for multiple nodes
        /// </summary>
        /// <param name="nodeIds">List of node IDs to consolidate</param>
        /// <param name="consolidationType">Type of consolidation</param>
        /// <param name="userId">User performing the consolidation</param>
        /// <returns>Bulk consolidation result</returns>
        Task<BulkConsolidationResult> BulkConsolidateAsync(List<string> nodeIds, ConsolidationType consolidationType, string userId);
    }
    
    public class ConsolidationResult
    {
        public bool Success { get; set; }
        public List<HierarchicalNode> ConsolidatedNodes { get; set; } = new List<HierarchicalNode>();
        public List<string> Errors { get; set; } = new List<string>();
        public int TotalNodesProcessed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
    
    public class BulkConsolidationResult
    {
        public bool Success { get; set; }
        public List<ConsolidationResult> IndividualResults { get; set; } = new List<ConsolidationResult>();
        public int TotalNodesProcessed { get; set; }
        public int SuccessfulConsolidations { get; set; }
        public int FailedConsolidations { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public List<string> GlobalErrors { get; set; } = new List<string>();
    }
}