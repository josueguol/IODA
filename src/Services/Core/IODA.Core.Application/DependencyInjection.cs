using FluentValidation;
using IODA.Core.Application.Behaviors;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Services;
using IODA.Core.Application.Validators.Schema;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IODA.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        // Validadores de esquema por tipo (orden: específicos primero, Default al final como fallback)
        services.AddSingleton<IFieldValidator, StringFieldValidator>();
        services.AddSingleton<IFieldValidator, NumberFieldValidator>();
        services.AddSingleton<IFieldValidator, BooleanFieldValidator>();
        services.AddSingleton<IFieldValidator, DateFieldValidator>();
        services.AddSingleton<IFieldValidator, EnumFieldValidator>();
        services.AddSingleton<IFieldValidator, DefaultFieldValidator>();

        services.AddTransient<ISchemaValidationService, SchemaValidationService>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
