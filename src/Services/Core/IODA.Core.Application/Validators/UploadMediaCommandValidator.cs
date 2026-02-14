using FluentValidation;
using IODA.Core.Application.Commands.Media;

namespace IODA.Core.Application.Validators;

/// <summary>
/// Valida UploadMediaCommand: proyecto, creador, nombre/tipo de archivo, tamaño y extensiones permitidas.
/// Coherente con RequestSizeLimit(50 MB) del MediaController.
/// </summary>
public class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    private const long MaxSizeBytes = 50L * 1024 * 1024; // 50 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf",
        ".mp4", ".webm", ".mp3", ".wav", ".ogg",
        ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".zip"
    };

    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.")
            .Must(HaveAllowedExtension).WithMessage("File extension is not allowed. Allowed: " + string.Join(", ", AllowedExtensions));

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0.")
            .LessThanOrEqualTo(MaxSizeBytes).WithMessage($"File size must not exceed {MaxSizeBytes / (1024 * 1024)} MB.");
    }

    private static bool HaveAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }
}
