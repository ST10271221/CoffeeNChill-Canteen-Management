using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace CoffeeNChillCanteen.Services;

public interface IBlobStorageService
{
    Task UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlobItem>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}