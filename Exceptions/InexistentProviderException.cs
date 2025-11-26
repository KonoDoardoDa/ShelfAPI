namespace ShelfApi.Exceptions
{
    public class InexistentProviderException : Exception
    {
        public InexistentProviderException(string message) : base(message) { }

        public InexistentProviderException(string message, Exception innerException) : base(message, innerException) { }
    }
}
