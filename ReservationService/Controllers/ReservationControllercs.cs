using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.DTO;
using ReservationService.Services.Interfaces;

namespace ReservationService.Controllers
{
	[Route("api/reservation")]
	[ApiController]
	public class ReservationControllercs(IReservationService reservationService) : ControllerBase
	{
		[Authorize(Roles = "Guest")]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateReservationRequestDTO reservationRequest, CancellationToken ct)
		{
			await reservationService.CreateAsync(reservationRequest, ct);
			return StatusCode(StatusCodes.Status201Created);
		}
	}
}
