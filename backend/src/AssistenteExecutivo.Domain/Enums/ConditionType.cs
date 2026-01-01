namespace AssistenteExecutivo.Domain.Enums;

/// <summary>
/// Defines the types of conditions for workflow branching.
/// </summary>
public enum ConditionType
{
    /// <summary>
    /// Values are exactly equal.
    /// </summary>
    Equals = 1,

    /// <summary>
    /// Values are not equal.
    /// </summary>
    NotEquals = 2,

    /// <summary>
    /// Left value contains right value (string).
    /// </summary>
    Contains = 3,

    /// <summary>
    /// Left value is greater than right value (numeric).
    /// </summary>
    GreaterThan = 4,

    /// <summary>
    /// Left value is less than right value (numeric).
    /// </summary>
    LessThan = 5,

    /// <summary>
    /// Value is null or empty.
    /// </summary>
    IsEmpty = 6,

    /// <summary>
    /// Value is not null and not empty.
    /// </summary>
    IsNotEmpty = 7,

    /// <summary>
    /// Date is before the specified date.
    /// </summary>
    DateBefore = 8,

    /// <summary>
    /// Date is after the specified date.
    /// </summary>
    DateAfter = 9
}
