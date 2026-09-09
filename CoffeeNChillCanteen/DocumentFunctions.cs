using CoffeeNChillCanteen.Services;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")]
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
}