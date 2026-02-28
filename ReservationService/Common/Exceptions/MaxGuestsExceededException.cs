namespace ReservationService.Common.Exceptions
{
	public class MaxGuestsExceededException : Exception
	{
		public MaxGuestsExceededException(string message) : base(message) { }
		public MaxGuestsExceededException(string message, Exception? innerException) : base(message, innerException) { }
	}
}
