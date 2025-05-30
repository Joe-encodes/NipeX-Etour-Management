using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using e_tour_api.Services;
using e_tour_api.Models.DTOs;
using e_tour_api.Models;

namespace e_tour_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentApprovalsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentApprovalsController> _logger;

        public DocumentApprovalsController(IDocumentService documentService, ILogger<DocumentApprovalsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("{documentId}/assign")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResult<DocumentApprovalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignApprover(int documentId, [FromBody] AssignApproverRequest request)
        {
            try
            {
                var result = await _documentService.AssignApproverAsync(
                    documentId,
                    request.ApproverId,
                    request.SignaturePositionX,
                    request.SignaturePositionY,
                    request.SignaturePage,
                    request.Comments
                );

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                if (result.Data == null)
                    return StatusCode(500, ServiceResult<object>.Error("No data returned from service", 500));

                return Ok(ServiceResult<DocumentApprovalDto>.Ok(result.Data, "Approver assigned successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning approver for document {DocumentId}", documentId);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while assigning the approver", 500));
            }
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(ServiceResult<List<DocumentApprovalDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingApprovals()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var result = await _documentService.GetPendingApprovalsAsync(userId);
                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                if (result.Data == null)
                    return StatusCode(500, ServiceResult<object>.Error("No data returned from service", 500));

                return Ok(ServiceResult<List<DocumentApprovalDto>>.Ok(result.Data, "Pending approvals retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals");
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while retrieving pending approvals", 500));
            }
        }

        [HttpPost("{documentId}/approve")]
        [ProducesResponseType(typeof(ServiceResult<DocumentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveDocument(int documentId, [FromBody] ApproveDocumentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var result = await _documentService.ApproveDocumentAsync(
                    documentId,
                    userId,
                    request.Signature,
                    request.Password
                );

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                if (result.Data == null)
                    return StatusCode(500, ServiceResult<object>.Error("No data returned from service", 500));

                return Ok(ServiceResult<DocumentDto>.Ok(result.Data, "Document approved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", documentId);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while approving the document", 500));
            }
        }

        [HttpPost("{documentId}/reject")]
        [ProducesResponseType(typeof(ServiceResult<DocumentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectDocument(int documentId, [FromBody] RejectDocumentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ServiceResult<object>.Error("Invalid user ID in token", 401));
                }

                var result = await _documentService.RejectDocumentAsync(
                    documentId,
                    userId,
                    request.Reason
                );

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, result);
                }

                if (result.Data == null)
                    return StatusCode(500, ServiceResult<object>.Error("No data returned from service", 500));

                return Ok(ServiceResult<DocumentDto>.Ok(result.Data, "Document rejected successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", documentId);
                return StatusCode(500, ServiceResult<object>.Error("An error occurred while rejecting the document", 500));
            }
        }
    }

    public class AssignApproverRequest
    {
        public int ApproverId { get; set; }
        public float SignaturePositionX { get; set; }
        public float SignaturePositionY { get; set; }
        public int SignaturePage { get; set; }
        public string? Comments { get; set; }
    }

    public class ApproveDocumentRequest
    {
        public string? Signature { get; set; }
        public string? Password { get; set; }
    }

    public class RejectDocumentRequest
    {
        public string? Reason { get; set; }
    }
} 