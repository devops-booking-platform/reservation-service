namespace ReservationService.Common.Exceptions
{
	public class PastDateException : Exception
	{
		public PastDateException(string message) : base(message) { }
		public PastDateException(string message, Exception? innerException) : base(message, innerException) { }
	}
}
