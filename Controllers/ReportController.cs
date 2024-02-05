using DSTV3.UploadInterface.Api.Models.UploadModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DSTV3.UploadInterface.Api.MongoGenericRepository;
using DSTV3.UploadInterface.Api.Entities;
using Microsoft.AspNetCore.Authorization;

namespace DSTV3.UploadInterface.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IMongoRepository<DFile> fileRepository;

        public ReportController(IConfiguration configuration, IMongoRepository<DFile> fileRepository)
        {
            this.configuration = configuration;
            this.fileRepository = fileRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public DuplicateTotalModel DuplicateTotal()
        {
            var convertToMB = Math.Pow(1024, 2);

            var totalSize = Math.Round(fileRepository.SumTotal(a => a.Size) / convertToMB, 2).ToString();
            var totalCount = fileRepository.CountTotal().ToString();
            var optimizedVolume = Math.Round(fileRepository.FindDuplicateDocuments().Sum(a => a.Size) / convertToMB, 2).ToString();
            var nnsavedDuplicateFiles = fileRepository.FindDuplicateDocuments().Count.ToString();

            DuplicateTotalModel duplicateTotal = new()
            {
                TotalSize = totalSize,
                TotalCount = totalCount,
                OptimizedVolume = optimizedVolume,
                UnsavedDuplicateFiles = nnsavedDuplicateFiles,
            };

            return duplicateTotal;
        }
    }
}
