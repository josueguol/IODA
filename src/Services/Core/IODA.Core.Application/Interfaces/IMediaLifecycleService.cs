using IODA.Core.Application.DTOs;

namespace IODA.Core.Application.Interfaces;

public interface IMediaLifecycleService
{
    Task<MediaLifecycleCleanupReport> CleanupOrphanedFilesAsync(
        Guid projectId,
        bool dryRun,
        int maxDeletes,
        CancellationToken cancellationToken = default);
}
