using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HierarchicalConsolidation.Models;

namespace HierarchicalConsolidation.Services
{
    /// <summary>
    /// Service for handling hierarchical consolidation operations with bulk processing capabilities
    /// </summary>
    public class HierarchicalConsolidationService : IHierarchicalConsolidationService
    {
        private readonly IDbContext _context;
        private readonly ILogger<HierarchicalConsolidationService> _logger;
        
        public HierarchicalConsolidationService(IDbContext context, ILogger<HierarchicalConsolidationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Consolidates a node and all its ancestors in a single bulk operation
        /// This implements the core logic: when G is consolidated, F, E, C, and A are also consolidated
        /// </summary>
        public async Task<ConsolidationResult> ConsolidateNodeWithAncestorsAsync(string nodeId, ConsolidationType consolidationType, string userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ConsolidationResult();
            
            try
            {
                _logger.LogInformation($"Starting consolidation for node {nodeId} by user {userId}");
                
                // Validate input
                if (string.IsNullOrEmpty(nodeId))
                {
                    result.Errors.Add("Node ID cannot be null or empty");
                    return result;
                }
                
                // Validate consolidation is allowed
                var validationResult = await ValidateConsolidationAsync(nodeId);
                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.ValidationErrors);
                    return result;
                }
                
                // Get the complete hierarchy path from root to the target node
                var hierarchyPath = await GetHierarchyPathAsync(nodeId);
                
                if (!hierarchyPath.Any())
                {
                    result.Errors.Add($"Node {nodeId} not found in hierarchy");
                    return result;
                }
                
                // Start transaction for bulk operation
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    var consolidatedNodes = new List<HierarchicalNode>();
                    var currentTime = DateTime.UtcNow;
                    
                    // Process nodes in bulk - from target node up to root
                    // This ensures all ancestors are consolidated when a child is consolidated
                    foreach (var node in hierarchyPath.Reverse()) // Reverse to go from target to root
                    {
                        if (!node.IsConsolidated)
                        {
                            node.IsConsolidated = true;
                            node.ConsolidationType = node.Id == nodeId ? consolidationType : ConsolidationType.Inherited;
                            node.ConsolidatedDate = currentTime;
                            node.ConsolidatedBy = userId;
                            node.ModifiedDate = currentTime;
                            node.ModifiedBy = userId;
                            
                            consolidatedNodes.Add(node);
                            
                            _logger.LogDebug($"Marked node {node.Id} ({node.Name}) for consolidation");
                        }
                    }
                    
                    // Bulk update all modified nodes
                    if (consolidatedNodes.Any())
                    {
                        _context.HierarchicalNodes.UpdateRange(consolidatedNodes);
                        await _context.SaveChangesAsync();
                    }
                    
                    await transaction.CommitAsync();
                    
                    result.Success = true;
                    result.ConsolidatedNodes = consolidatedNodes;
                    result.TotalNodesProcessed = consolidatedNodes.Count;
                    
                    _logger.LogInformation($"Successfully consolidated {consolidatedNodes.Count} nodes in hierarchy for node {nodeId}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error during consolidation transaction for node {nodeId}");
                    result.Errors.Add($"Transaction failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error consolidating node {nodeId}");
                result.Errors.Add($"Consolidation failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all ancestors of a given node (parent, grandparent, etc.)
        /// </summary>
        public async Task<List<HierarchicalNode>> GetAncestorsAsync(string nodeId)
        {
            var ancestors = new List<HierarchicalNode>();
            
            try
            {
                var currentNode = await _context.HierarchicalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId);
                
                while (currentNode?.ParentId != null)
                {
                    var parent = await _context.HierarchicalNodes
                        .FirstOrDefaultAsync(n => n.Id == currentNode.ParentId);
                    
                    if (parent != null)
                    {
                        ancestors.Add(parent);
                        currentNode = parent;
                    }
                    else
                    {
                        break; // Prevent infinite loop if parent is not found
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting ancestors for node {nodeId}");
            }
            
            return ancestors;
        }
        
        /// <summary>
        /// Gets the complete hierarchy path from root to the specified node
        /// </summary>
        public async Task<List<HierarchicalNode>> GetHierarchyPathAsync(string nodeId)
        {
            var path = new List<HierarchicalNode>();
            
            try
            {
                // Get the target node
                var targetNode = await _context.HierarchicalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId);
                
                if (targetNode == null)
                {
                    return path;
                }
                
                // Build path from target to root
                var currentNode = targetNode;
                path.Add(currentNode);
                
                // Get all ancestors
                var ancestors = await GetAncestorsAsync(nodeId);
                path.AddRange(ancestors);
                
                // Return path from root to target (reverse the list)
                path.Reverse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting hierarchy path for node {nodeId}");
            }
            
            return path;
        }
        
        /// <summary>
        /// Validates if consolidation is allowed for the given node
        /// </summary>
        public async Task<ValidationResult> ValidateConsolidationAsync(string nodeId)
        {
            var result = new ValidationResult { IsValid = true };
            
            try
            {
                var node = await _context.HierarchicalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId);
                
                if (node == null)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add($"Node with ID {nodeId} not found");
                    return result;
                }
                
                // Add business rules for validation
                if (node.IsConsolidated)
                {
                    result.ValidationErrors.Add($"Node {nodeId} is already consolidated");
                    // Note: This might be a warning rather than an error depending on business rules
                }
                
                // Add more validation rules as needed
                // Example: Check if user has permission to consolidate
                // Example: Check if node is in a valid state for consolidation
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating consolidation for node {nodeId}");
                result.IsValid = false;
                result.ValidationErrors.Add($"Validation failed: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Performs bulk consolidation for multiple nodes with optimized batch processing
        /// </summary>
        public async Task<BulkConsolidationResult> BulkConsolidateAsync(List<string> nodeIds, ConsolidationType consolidationType, string userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var bulkResult = new BulkConsolidationResult();
            
            try
            {
                _logger.LogInformation($"Starting bulk consolidation for {nodeIds.Count} nodes by user {userId}");
                
                if (nodeIds == null || !nodeIds.Any())
                {
                    bulkResult.GlobalErrors.Add("No node IDs provided for bulk consolidation");
                    return bulkResult;
                }
                
                // Process nodes in batches to avoid memory issues with large datasets
                const int batchSize = 100;
                var batches = nodeIds.Chunk(batchSize);
                
                foreach (var batch in batches)
                {
                    var batchTasks = batch.Select(nodeId => 
                        ConsolidateNodeWithAncestorsAsync(nodeId, consolidationType, userId)
                    );
                    
                    var batchResults = await Task.WhenAll(batchTasks);
                    bulkResult.IndividualResults.AddRange(batchResults);
                }
                
                // Calculate summary statistics
                bulkResult.SuccessfulConsolidations = bulkResult.IndividualResults.Count(r => r.Success);
                bulkResult.FailedConsolidations = bulkResult.IndividualResults.Count(r => !r.Success);
                bulkResult.TotalNodesProcessed = bulkResult.IndividualResults.Sum(r => r.TotalNodesProcessed);
                bulkResult.Success = bulkResult.FailedConsolidations == 0;
                
                _logger.LogInformation($"Bulk consolidation completed. Success: {bulkResult.SuccessfulConsolidations}, Failed: {bulkResult.FailedConsolidations}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk consolidation");
                bulkResult.GlobalErrors.Add($"Bulk consolidation failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                bulkResult.TotalProcessingTime = stopwatch.Elapsed;
            }
            
            return bulkResult;
        }
    }
    
    // Extension method for chunking (if not available in your .NET version)
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            var chunk = new List<T>(chunkSize);
            foreach (var item in source)
            {
                chunk.Add(item);
                if (chunk.Count == chunkSize)
                {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }
            if (chunk.Any())
            {
                yield return chunk;
            }
        }
    }
}