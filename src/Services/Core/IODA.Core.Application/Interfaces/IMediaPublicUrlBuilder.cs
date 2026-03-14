namespace IODA.Core.Application.Interfaces;

public interface IMediaPublicUrlBuilder
{
    string BuildFileUrl(Guid projectId, Guid mediaId, string? variant = null);
}
