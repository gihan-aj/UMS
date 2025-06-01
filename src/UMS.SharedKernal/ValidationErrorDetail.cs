namespace UMS.SharedKernel
{
    /// <summary>
    /// Details of a specific validation error, typically for a single property.
    /// </summary>
    /// <param name="PropertyName">The name of the property that failed validation.</param>
    /// <param name="ErrorMessage">The error message for this specific property.</param>
    public sealed record ValidationErrorDetail(string PropertyName, string ErrorMessage);
}
