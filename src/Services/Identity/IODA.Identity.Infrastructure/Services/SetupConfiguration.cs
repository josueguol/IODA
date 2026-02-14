using IODA.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IODA.Identity.Infrastructure.Services;

public class SetupConfiguration : ISetupConfiguration
{
    private readonly IConfiguration _configuration;

    public SetupConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool SelfRegistrationEnabled =>
        bool.TryParse(_configuration["SelfRegistration:Enabled"], out var enabled) ? enabled : true;
}
