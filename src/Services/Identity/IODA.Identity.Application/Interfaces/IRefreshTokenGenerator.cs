namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Genera un token de refresco (string) y su validez.
/// </summary>
public interface IRefreshTokenGenerator
{
    (string Token, TimeSpan Validity) Generate();
}
