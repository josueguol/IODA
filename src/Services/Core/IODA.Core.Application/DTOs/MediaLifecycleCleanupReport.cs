namespace IODA.Core.Application.DTOs;

public record MediaLifecycleCleanupReport(
    Guid ProjectId,
    bool DryRun,
    int ReferencedKeys,
    int StorageKeys,
    int OrphanKeys,
    int DeletedKeys,
    IReadOnlyList<string> SampleOrphanKeys,
    IReadOnlyList<string> SampleDeletedKeys);
