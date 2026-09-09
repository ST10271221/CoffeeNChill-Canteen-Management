using CoffeeNChillCanteen.Services;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CoffeeNChillCanteen;

public class DocumentFunctions
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DocumentFunctions> _logger;

    public DocumentFunctions(
        IBlobStorageService blobStorageService,
        ILogger<DocumentFunctions> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [Function("UploadStaffDocument")]
    public async Task<IActionResult> UploadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "documents/upload")]
        HttpRequest req)
    {
        _logger.LogInformation(
            "Staff document upload request received.");

        if (!req.HasFormContentType)
        {
            _logger.LogWarning(
                "Upload rejected because the request was not multipart/form-data.");

            return new BadRequestObjectResult(new
            {
                error = "Request must use multipart/form-data."
            });
        }

        try
        {
            var parser = await MultipartFormDataParser.ParseAsync(
                req.Body);

            if (parser.Files.Count == 0)
            {
                _logger.LogWarning(
                    "Upload rejected because no file was provided.");

                return new BadRequestObjectResult(new
                {
                    error = "No file was provided."
                });
            }

            var file = parser.Files.First();

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                _logger.LogWarning(
                    "Upload rejected because the file name was missing.");

                return new BadRequestObjectResult(new
                {
                    error = "The uploaded file must have a file name."
                });
            }

            var extension = Path.GetExtension(file.FileName);

            if (!extension.Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Upload rejected for unsupported file type: {FileName}",
                    file.FileName);

                return new BadRequestObjectResult(new
                {
                    error = "Only PDF documents are allowed."
                });
            }

            var safeFileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                return new BadRequestObjectResult(new
                {
                    error = "Invalid file name."
                });
            }

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/pdf"
                : file.ContentType;

            if (!contentType.Equals(
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Upload rejected because the content type was {ContentType}.",
                    contentType);

                return new BadRequestObjectResult(new
                {
                    error = "The uploaded document must have content type application/pdf."
                });
            }

            await _blobStorageService.UploadAsync(
                file.Data,
                safeFileName,
                "application/pdf");

            _logger.LogInformation(
                "Staff document uploaded successfully: {FileName}",
                safeFileName);

            return new OkObjectResult(new
            {
                message = "Staff document uploaded successfully.",
                fileName = safeFileName,
                contentType = "application/pdf"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while uploading a staff document.");

            return new ObjectResult(new
            {
                error = "The staff document could not be uploaded."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("ListStaffDocuments")]
    public async Task<IActionResult> ListStaffDocuments(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "documents")]
        HttpRequest req)
    {
        _logger.LogInformation(
            "Staff document listing request received.");

        try
        {
            var blobs = await _blobStorageService.ListAsync();

            var documents = blobs
                .Select(blob => new
                {
                    fileName = blob.Name,
                    size = blob.Properties.ContentLength ?? 0,
                    lastModified = blob.Properties.LastModified
                })
                .OrderBy(document => document.fileName)
                .ToList();

            _logger.LogInformation(
                "Retrieved {DocumentCount} staff documents.",
                documents.Count);

            return new OkObjectResult(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while listing staff documents.");

            return new ObjectResult(new
            {
                error = "Staff documents could not be retrieved."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("DownloadStaffDocument")]
    public async Task<IActionResult> DownloadStaffDocument(
    [HttpTrigger(
        AuthorizationLevel.Anonymous,
        "get",
        Route = "documents/download/{fileName}")]
    HttpRequest req,
    string fileName)
    {
        _logger.LogInformation(
            "Staff document download request received for: {FileName}",
            fileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            _logger.LogWarning(
                "Download rejected because the file name was empty.");

            return new BadRequestObjectResult(new
            {
                error = "A file name is required."
            });
        }

        var safeFileName = Path.GetFileName(fileName);

        if (!string.Equals(
                safeFileName,
                fileName,
                StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Download rejected because an unsafe file name was supplied: {FileName}",
                fileName);

            return new BadRequestObjectResult(new
            {
                error = "Invalid file name."
            });
        }

        try
        {
            var documentStream = await _blobStorageService.DownloadAsync(
                safeFileName);

            if (documentStream is null)
            {
                _logger.LogWarning(
                    "Staff document not found: {FileName}",
                    safeFileName);

                return new NotFoundObjectResult(new
                {
                    error = "The requested staff document was not found.",
                    fileName = safeFileName
                });
            }

            _logger.LogInformation(
                "Staff document downloaded successfully: {FileName}",
                safeFileName);

            return new FileStreamResult(
                documentStream,
                "application/pdf")
            {
                FileDownloadName = safeFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while downloading staff document: {FileName}",
                safeFileName);

            return new ObjectResult(new
            {
                error = "The staff document could not be downloaded."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}