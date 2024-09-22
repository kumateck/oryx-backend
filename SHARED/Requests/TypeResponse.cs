namespace SHARED.Requests;

/// <summary>
/// Represents a Question Type with its numeric value and string name.
/// </summary>
public class TypeResponse
{
    /// <summary>
    /// The numeric value of the Question Type. This is what should be passed to the API.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// The string name of the Question Type.
    /// </summary>
    public string Name { get; set; }
}