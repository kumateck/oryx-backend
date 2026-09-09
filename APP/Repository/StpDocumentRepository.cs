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

public partial class StpDocumentRepository : IStpDocumentRepository
{
    private const string BucketName = "stp-documents";
    private const string WordMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext context;
    private readonly IBlobStorageService blobStorageService;
    private readonly IOnlyOfficeConfigService onlyOfficeConfigService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly UserManager<User> userManager;
    private readonly IMapper mapper;
    private readonly ILogger<StpDocumentRepository> logger;

    public StpDocumentRepository(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        IOnlyOfficeConfigService onlyOfficeConfigService,
        IHttpClientFactory httpClientFactory,
        UserManager<User> userManager,
        IMapper mapper,
        ILogger<StpDocumentRepository> logger)
    {
        this.context = context;
        this.blobStorageService = blobStorageService;
        this.onlyOfficeConfigService = onlyOfficeConfigService;
        this.httpClientFactory = httpClientFactory;
        this.userManager = userManager;
        this.mapper = mapper;
        this.logger = logger;
    }

}
