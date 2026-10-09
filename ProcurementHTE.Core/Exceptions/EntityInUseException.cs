namespace ProcurementHTE.Core.Exceptions;

/// <summary>
/// Thrown when a record cannot be deleted because active data still refers to it.
/// The message is written for end users.
/// </summary>
public sealed class EntityInUseException(string message) : InvalidOperationException(message);
