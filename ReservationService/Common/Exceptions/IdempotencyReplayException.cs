namespace ReservationService.Common.Exceptions
{
	public class IdempotencyReplayException : Exception
	{
		public IdempotencyReplayException(string message) : base(message) { }
		public IdempotencyReplayException(string message, Exception? innerException) : base(message, innerException) { }
	}
}
