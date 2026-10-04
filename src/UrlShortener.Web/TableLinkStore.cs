using System.Net;
using Azure;
using Azure.Data.Tables;

namespace UrlShortener.Web;

/// <summary>
/// An <see cref="ILinkStore"/> backed by Azure Table Storage. Each slug is its own partition, which keeps
/// point reads and conditional inserts to a single, cheap transaction.
/// </summary>
public sealed class TableLinkStore : ILinkStore
{
    /// <summary>The table that holds link entities.</summary>
    public const string TableName = "links";

    private const string UrlProperty = "Url";

    private readonly TableClient _table;
    private volatile bool _tableExists;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableLinkStore"/> class.
    /// </summary>
    /// <param name="serviceClient">The table service client.</param>
    public TableLinkStore(TableServiceClient serviceClient)
    {
        ArgumentNullException.ThrowIfNull(serviceClient);
        _table = serviceClient.GetTableClient(TableName);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            NullableResponse<TableEntity> response = await _table
                .GetEntityIfExistsAsync<TableEntity>(slug, string.Empty, [UrlProperty], cancellationToken)
                .ConfigureAwait(false);

            return response.HasValue ? response.Value!.GetString(UrlProperty) : null;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            // The table is created lazily on first write, so reads before then report TableNotFound.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> TryAddAsync(string slug, string url, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var entity = new TableEntity(slug, string.Empty) { [UrlProperty] = url };
        try
        {
            await _table.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            return await GetAsync(slug, cancellationToken).ConfigureAwait(false) ?? string.Empty;
        }
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_tableExists)
        {
            return;
        }

        await _table.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
        _tableExists = true;
    }
}
