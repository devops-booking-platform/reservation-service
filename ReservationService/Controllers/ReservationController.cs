using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.DTO;
using ReservationService.Services.Interfaces;

namespace ReservationService.Controllers
{
	[Route("api/reservation")]
	[ApiController]
	public class ReservationController(IReservationService reservationService) : ControllerBase
	{
		[Authorize(Roles = "Guest")]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateReservationRequestDTO reservationRequest, CancellationToken ct)
		{
			if (!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues) ||
		!Guid.TryParse(keyValues.ToString(), out var idempotencyKey) ||
		idempotencyKey == Guid.Empty)
			{
				return BadRequest("Missing or invalid Idempotency-Key header.");
			}
			await reservationService.CreateAsync(reservationRequest, idempotencyKey, ct);
			return StatusCode(StatusCodes.Status201Created);
		}
	}
}
