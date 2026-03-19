# Hierarchical Consolidation Service

This service implements hierarchical consolidation logic with bulk operations. When a node is consolidated, all its ancestors in the hierarchy are automatically consolidated as well.

## Architecture Overview

```
A (Root)
├── B
├── C
│   ├── E
│   ├── F
│   ├── G  ← When G is consolidated
│   └── y
└── D
```

When node G is consolidated, the following nodes are automatically consolidated:
- G (the target node)
- C (parent of G)
- A (root parent)

## Key Features

### 1. Hierarchical Consolidation
- **Automatic Ancestor Consolidation**: When a child node is consolidated, all ancestors are automatically consolidated
- **Bulk Operations**: Optimized for processing multiple nodes efficiently
- **Transaction Safety**: All operations are wrapped in database transactions
- **Audit Trail**: Complete tracking of who consolidated what and when

### 2. Performance Optimizations
- **Batch Processing**: Large datasets are processed in configurable batches
- **Bulk Database Operations**: Uses Entity Framework's bulk update capabilities
- **Efficient Queries**: Optimized database queries with proper indexing
- **Async Operations**: All operations are asynchronous for better scalability

### 3. Error Handling
- **Comprehensive Validation**: Validates nodes before consolidation
- **Transaction Rollback**: Automatic rollback on errors
- **Detailed Error Reporting**: Specific error messages for troubleshooting
- **Logging**: Comprehensive logging for monitoring and debugging

## Usage Examples

### Single Node Consolidation
```csharp
var result = await consolidationService.ConsolidateNodeWithAncestorsAsync(
    "G", 
    ConsolidationType.Manual, 
    "user123"
);
```

### Bulk Consolidation
```csharp
var nodeIds = new List<string> { "G", "F", "E" };
var result = await consolidationService.BulkConsolidateAsync(
    nodeIds, 
    ConsolidationType.Manual, 
    "user123"
);
```

### API Endpoints

#### POST /api/consolidation/consolidate
Consolidates a single node and its ancestors.

```json
{
  "nodeId": "G",
  "consolidationType": 1,
  "userId": "user123"
}
```

#### POST /api/consolidation/bulk-consolidate
Consolidates multiple nodes in bulk.

```json
{
  "nodeIds": ["G", "F", "E"],
  "consolidationType": 1,
  "userId": "user123"
}
```

#### GET /api/consolidation/hierarchy/{nodeId}
Returns the complete hierarchy path from root to the specified node.

#### GET /api/consolidation/validate/{nodeId}
Validates if consolidation is allowed for the specified node.

## Configuration

### Database Setup
```csharp
services.AddDbContext<HierarchicalDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IDbContext, HierarchicalDbContext>();
services.AddScoped<IHierarchicalConsolidationService, HierarchicalConsolidationService>();
```

### Batch Size Configuration
The service processes nodes in batches of 100 by default. This can be modified in the `BulkConsolidateAsync` method:

```csharp
const int batchSize = 100; // Adjust as needed
```

## Database Schema

The `HierarchicalNode` table includes:
- **Id**: Primary key
- **Name**: Display name
- **ParentId**: Foreign key to parent node
- **IsConsolidated**: Consolidation status
- **ConsolidationType**: Type of consolidation (Manual, Automatic, Inherited)
- **ConsolidatedDate**: When consolidation occurred
- **ConsolidatedBy**: User who performed consolidation
- **Audit fields**: Created/Modified dates and users

### Indexes
- `IX_HierarchicalNode_ParentId`: For efficient parent lookups
- `IX_HierarchicalNode_IsConsolidated`: For filtering consolidated nodes
- `IX_HierarchicalNode_ParentId_IsConsolidated`: Composite index for complex queries

## Core Logic Implementation

The main consolidation logic follows this approach:

1. **Input Validation**: Validate the target node ID and user permissions
2. **Hierarchy Path Discovery**: Build the complete path from root to target node
3. **Bulk Consolidation**: Mark all nodes in the path as consolidated in a single transaction
4. **Audit Trail**: Record consolidation metadata (who, when, type)
5. **Transaction Management**: Ensure atomicity with proper rollback on errors

### Key Algorithm Steps:

```csharp
// 1. Get hierarchy path (A -> C -> G)
var hierarchyPath = await GetHierarchyPathAsync(nodeId);

// 2. Process from target to root (G -> C -> A)
foreach (var node in hierarchyPath.Reverse())
{
    if (!node.IsConsolidated)
    {
        node.IsConsolidated = true;
        node.ConsolidationType = node.Id == nodeId ? consolidationType : ConsolidationType.Inherited;
        // ... set audit fields
    }
}

// 3. Bulk update in single transaction
_context.HierarchicalNodes.UpdateRange(consolidatedNodes);
await _context.SaveChangesAsync();
```

## Error Handling

The service provides comprehensive error handling:

1. **Validation Errors**: Invalid input parameters
2. **Business Logic Errors**: Consolidation rules violations
3. **Database Errors**: Connection issues, constraint violations
4. **Transaction Errors**: Rollback scenarios

All errors are logged and returned in structured result objects.

## Performance Considerations

1. **Memory Usage**: Large hierarchies are processed in batches
2. **Database Connections**: Uses connection pooling
3. **Transaction Scope**: Minimized transaction duration
4. **Parallel Processing**: Batch operations can run in parallel

## Testing

The service includes comprehensive unit tests covering:
- Single node consolidation
- Bulk operations
- Error scenarios
- Performance benchmarks

## Monitoring

Built-in logging provides insights into:
- Processing times
- Success/failure rates
- Error patterns
- Performance metrics

Use structured logging to integrate with monitoring systems like Application Insights or ELK stack.

## Repository Structure

```
├── HierarchicalNode.cs                    # Entity model
├── IHierarchicalConsolidationService.cs   # Service interface
├── HierarchicalConsolidationService.cs    # Main service implementation
├── IDbContext.cs                          # Database context interface
├── HierarchicalDbContext.cs               # Entity Framework context
├── ConsolidationController.cs             # API controller
├── ConsolidationServiceTests.cs           # Unit tests
└── README.md                              # This file
```

This implementation provides a robust, scalable solution for hierarchical consolidation with proper error handling, performance optimization, and comprehensive testing.