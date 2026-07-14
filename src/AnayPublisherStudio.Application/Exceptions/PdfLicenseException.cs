namespace AnayPublisherStudio.Application.Exceptions;

public sealed class PdfLicenseException : InvalidOperationException
{
    public PdfLicenseException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
