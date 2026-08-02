namespace Nami.Identity.Abstractions;

/// <summary>
/// A scope as an operator declares it, before it becomes an engine descriptor.
/// </summary>
/// <remarks>
/// The three members and their nullability are transcribed from the class
/// diagram in design 23 section 3, which is the implementer source of record for
/// the definition model. That diagram annotates nullable members explicitly
/// elsewhere in the same block, so an unannotated member here is non-nullable by
/// statement rather than by assumption.
/// </remarks>
public sealed class ScopeDefinition
{
    /// <summary>The scope name, as it appears on the wire.</summary>
    public required string Name { get; set; }

    /// <summary>The human-readable name shown on a consent screen.</summary>
    public required string DisplayName { get; set; }

    /// <summary>The resources this scope grants access to.</summary>
    public required string[] Resources { get; set; }
}
