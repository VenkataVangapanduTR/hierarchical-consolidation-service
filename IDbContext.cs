using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HierarchicalConsolidation.Models;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchicalConsolidation.Services
{
    /// <summary>
    /// Database context interface for hierarchical consolidation
    /// </summary>
    public interface IDbContext
    {
        DbSet<HierarchicalNode> HierarchicalNodes { get; set; }
        
        DatabaseFacade Database { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}