using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace CoffeeNChillCanteen.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(string connectionString)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);

        _containerClient = blobServiceClient.GetBlobContainerClient(
            "staff-docs");
    }

    public async Task UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var blobClient = _containerClient.GetBlobClient(fileName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Conditions = new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            }
        };

        await blobClient.UploadAsync(
            content,
            options,
            cancellationToken);
    }

    public async Task<IReadOnlyList<BlobItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var results = new List<BlobItem>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(
            cancellationToken: cancellationToken))
        {
            results.Add(blobItem);
        }

        return results;
    }

    public async Task<Stream?> DownloadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(
            cancellationToken: cancellationToken);

        return response.Value.Content;
    }
}