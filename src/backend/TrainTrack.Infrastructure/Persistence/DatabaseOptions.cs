using System.ComponentModel.DataAnnotations;

namespace TrainTrack.Infrastructure.Persistence;

/// <summary>Konfiguracja bazy; connection string pochodzi z <c>ConnectionStrings:Database</c> (user-secrets lub zmienna środowiskowa).</summary>
public sealed class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
