using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.DTO;
using ReservationService.Services.Interfaces;

namespace ReservationService.Controllers
{
	[Route("api/reservations")]
	[ApiController]
	public class ReservationController(IReservationService reservationService) : ControllerBase
	{
        [Authorize(Roles = "Guest")]
        [HttpPost]
        public async Task<IActionResult> Create(
        [FromBody] CreateReservationRequest reservationRequest,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken ct)
        {
            if (idempotencyKey == Guid.Empty)
                return BadRequest("Missing or invalid Idempotency-Key header.");

            await reservationService.CreateAsync(reservationRequest, idempotencyKey, ct);
            return StatusCode(StatusCodes.Status201Created);
        }

        [Authorize(Roles = "Guest")]
        [HttpGet("approved")]
        public async Task<IActionResult> GetApproved(CancellationToken ct) 
        {
			var approvedReservations = await reservationService.GetApprovedForGuestAsync(ct);
			return Ok(approvedReservations);
        }

		[Authorize(Roles = "Guest")]
		[HttpPatch("{reservationId:guid}/cancel")]
		public async Task<IActionResult> Cancel([FromRoute] Guid reservationId, CancellationToken ct)
		{
			await reservationService.CancelAsync(reservationId, ct);
			return NoContent();
		}
		
		[Authorize(Roles = "Host")]
		[HttpPost("approve/{reservationId}")]
		public async Task<IActionResult> ConfirmReservation(Guid reservationId, CancellationToken ct)
		{
			await reservationService.ApproveAsync(reservationId, ct);
			return StatusCode(StatusCodes.Status202Accepted);
		}
		
		[Authorize(Roles = "Host")]
		[HttpPost("decline/{reservationId}")]
		public async Task<IActionResult> DeclineReservation(Guid reservationId, CancellationToken ct)
		{
			await reservationService.DeclineAsync(reservationId, ct);
			return StatusCode(StatusCodes.Status202Accepted);
		}
	}
}
