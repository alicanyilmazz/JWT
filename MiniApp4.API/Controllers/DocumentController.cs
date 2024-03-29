﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp4.Core.Dtos;
using MiniApp4.Core.Entities;
using MiniApp4.Core.Services;
using SharedLibrary.Dtos;
using System.Security.Claims;

namespace MiniApp4.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DocumentController : CustomBaseController
    {
        private readonly IService<Document, DocumentDto> _documentService;

        public DocumentController(IService<Document, DocumentDto> documentService)
        {
            _documentService = documentService;
        }

        [HttpPost]
        public async Task<IActionResult> SaveDocument(IFormFile document, CancellationToken cancellationToken)
        {
            if (document.Length > 3 * 1024 * 1024)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Document size can not be greater than 3 MB.", 400, true));
            }
            if (document != null && document.Length > 0)
            {
                var userName = HttpContext.User?.Identity?.Name;
                var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                string randomFileName = string.Empty;
                randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(document.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/documents", randomFileName);
                using var stream = new FileStream(path, FileMode.Create);
                await document.CopyToAsync(stream, cancellationToken);
                var documentInfo = new DocumentDto { Url = "documents/" + randomFileName, DocumentId = userName + '|' + userIdClaim?.Value };
                return ActionResultInstance(await _documentService.AddAsync(documentInfo));
            }
            return ActionResultInstance(Response<NoDataDto>.Fail("Documents can not be empty.", 400, true));
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteDocument(DocumentDto documentDeleteDto, CancellationToken cancellationToken)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", documentDeleteDto.Url);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return ActionResultInstance(await _documentService.Remove(documentDeleteDto.Id));
            }

            return ActionResultInstance(Response<NoDataDto>.Fail("Documents does not exist.", 400, true));
        }
    }
}
