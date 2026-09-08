using System.Security.Cryptography;
using System.Text.Json;
using APP.IRepository;
using APP.Services.OnlyOffice;
using APP.Services.Storage;
using AutoMapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.StpDocuments;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public partial class StpDocumentRepository
{
    private static Result ValidateDocxFile(IFormFile file)
    {
        if (file.Length == 0)
            return Result.Failure(StpDocumentErrors.EmptyFile);

        if (file.Length > MaxFileSizeBytes)
            return Result.Failure(StpDocumentErrors.FileTooLarge(MaxFileSizeBytes));

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(StpDocumentErrors.InvalidFileType);

        try
        {
            using var stream = file.OpenReadStream();
            using var document = WordprocessingDocument.Open(stream, false);
            if (document.MainDocumentPart?.Document == null)
                return Result.Failure(StpDocumentErrors.InvalidFileSignature);
        }
        catch (OpenXmlPackageException)
        {
            return Result.Failure(StpDocumentErrors.InvalidFileSignature);
        }
        catch (IOException)
        {
            return Result.Failure(StpDocumentErrors.InvalidFileSignature);
        }

        return Result.Success();
    }

    private static string ComputeSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static IFormFile WrapAsFormFile(byte[] bytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.LongLength, "file", fileName) { Headers = new HeaderDictionary(), ContentType = contentType };
    }

    private static byte[] GenerateBlankDocx()
    {
        using var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(string.Empty)))));
            mainPart.Document.Save();
        }
        return stream.ToArray();
    }
}
