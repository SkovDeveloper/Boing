using System.Text.Json;
using System.Text.Json.Serialization;

namespace Boing.Services;

public sealed class ProjectStorageService : IProjectStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public string AutoSavePath { get; }

    public ProjectStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Boing");
        Directory.CreateDirectory(folder);
        AutoSavePath = Path.Combine(folder, "autosave.boing");
    }

    public async Task SaveAsync<T>(string filePath, T data, CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

            await using (var stream =
                         new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task<T?> LoadAsync<T>(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            await using var stream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public Task SaveAutoSaveAsync<T>(T data, CancellationToken cancellationToken = default) =>
        SaveAsync(AutoSavePath, data, cancellationToken);

    public Task<T?> LoadAutoSaveAsync<T>(CancellationToken cancellationToken = default) =>
        LoadAsync<T>(AutoSavePath, cancellationToken);

    public void ClearAutoSave()
    {
        if (File.Exists(AutoSavePath))
        {
            File.Delete(AutoSavePath);
        }
    }

    public async Task<CostProject> LoadDefaultScenarioAsync()
    {
        var autoSaved = await LoadAutoSaveAsync<CostProject>();
        if (autoSaved is not null)
        {
            return autoSaved;
        }

        return new CostProject();
    }
}