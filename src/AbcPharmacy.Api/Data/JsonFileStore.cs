using System.Text.Json;

namespace AbcPharmacy.Api.Data;


public sealed class JsonFileStore<T> where T : class
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonFileStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    }

    public string FilePath { get; }

    public bool FileExists => File.Exists(FilePath);

    public async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    
    public async Task<TResult> UpdateAsync<TResult>(Func<List<T>, TResult> update, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadAsync(cancellationToken);
            var result = update(items);

           
            await SaveAsync(items, CancellationToken.None);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<T>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(FilePath);
        if (stream.Length == 0)
        {
            return [];
        }

        return await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, cancellationToken) ?? [];
    }

    private async Task SaveAsync(List<T> items, CancellationToken cancellationToken)
    {
        var tempPath = FilePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, FilePath, overwrite: true);
    }
}
