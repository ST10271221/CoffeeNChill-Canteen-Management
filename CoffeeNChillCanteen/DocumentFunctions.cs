using CoffeeNChillCanteen.Services;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Azure;

namespace CoffeeNChillCanteen;

public class DocumentFunctions
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;
    private const int MaxFileNameLength = 100;
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

        if (req.ContentLength.HasValue &&
        req.ContentLength.Value > MaxDocumentSizeBytes)
        {
            _logger.LogWarning(
                "Upload rejected because the request exceeded the maximum size of {MaxSizeBytes} bytes.",
                MaxDocumentSizeBytes);

            return new ObjectResult(new
            {
                error = "The uploaded document is too large.",
                maximumSizeBytes = MaxDocumentSizeBytes
            })
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge
            };
        }

        if (!req.HasFormContentType)
        {
            _logger.LogWarning(
                "Upload rejected because the request was not multipart/form-data.");

            return new BadRequestObjectResult(new
            {
                error = "Request must use multipart/form-data."
            });
        }

        string safeFileName = string.Empty;

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

            safeFileName = Path.GetFileName(file.FileName).Trim();

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                _logger.LogWarning(
                    "Upload rejected because the file name was invalid.");

                return new BadRequestObjectResult(new
                {
                    error = "Invalid file name."
                });
            }

            if (safeFileName.Length > MaxFileNameLength)
            {
                _logger.LogWarning(
                    "Upload rejected because the file name exceeded {MaxLength} characters.",
                    MaxFileNameLength);

                return new BadRequestObjectResult(new
                {
                    error = "The file name is too long.",
                    maximumFileNameLength = MaxFileNameLength
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

            if (file.Data is null)
            {
                _logger.LogWarning(
                    "Upload rejected because no file data was available.");

                return new BadRequestObjectResult(new
                {
                    error = "The uploaded document contains no data."
                });
            }

            if (file.Data.CanSeek &&
                file.Data.Length == 0)
            {
                _logger.LogWarning(
                    "Upload rejected because the document was empty.");

                return new BadRequestObjectResult(new
                {
                    error = "The uploaded document is empty."
                });
            }

            if (file.Data.CanSeek &&
                file.Data.Length > MaxDocumentSizeBytes)
            {
                _logger.LogWarning(
                    "Upload rejected because the document exceeded the maximum size.");

                return new ObjectResult(new
                {
                    error = "The uploaded document is too large.",
                    maximumSizeBytes = MaxDocumentSizeBytes
                })
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
            }

            if (file.Data.CanSeek)
            {
                var originalPosition = file.Data.Position;

                try
                {
                    var header = new byte[5];

                    file.Data.Position = 0;

                    var bytesRead = await file.Data.ReadAsync(
                        header,
                        0,
                        header.Length);

                    if (bytesRead < 5 ||
                        header[0] != 0x25 ||
                        header[1] != 0x50 ||
                        header[2] != 0x44 ||
                        header[3] != 0x46 ||
                        header[4] != 0x2D)
                    {
                        _logger.LogWarning(
                            "Upload rejected because the file does not have a valid PDF signature: {FileName}",
                            safeFileName);

                        return new BadRequestObjectResult(new
                        {
                            error = "The uploaded file is not a valid PDF document."
                        });
                    }
                }
                finally
                {
                    file.Data.Position = originalPosition;
                }
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
        catch (RequestFailedException ex)
        when (ex.Status == StatusCodes.Status409Conflict)
        {
            _logger.LogWarning(
                "Upload rejected because the document already exists: {FileName}",
                safeFileName);

            return new ConflictObjectResult(new
            {
                error = "A staff document with this file name already exists.",
                fileName = safeFileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while uploading a staff document: {FileName}",
                safeFileName);

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
                "An error occurred while downloading a staff document: {FileName}",
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