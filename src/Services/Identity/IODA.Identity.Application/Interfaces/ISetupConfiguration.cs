namespace IODA.Identity.Application.Interfaces;

/// <summary>Configuración de setup del sistema (auto-registro, etc.). Abstraction para no acoplar Application a IConfiguration.</summary>
public interface ISetupConfiguration
{
    bool SelfRegistrationEnabled { get; }
}
