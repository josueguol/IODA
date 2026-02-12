namespace IODA.Publishing.Application.Interfaces;

/// <summary>
/// Cliente para llamar al Core API y publicar contenido.
/// </summary>
public interface ICorePublishClient
{
    Task PublishAsync(Guid projectId, Guid contentId, Guid publishedBy, CancellationToken cancellationToken = default);
}
