namespace Nami.Identity.Abstractions;

/// <summary>
/// The two hashes an append produced, returned so a caller can assert that the append
/// actually happened rather than assuming it.
/// </summary>
/// <remarks>
/// <para>
/// Both members are transcribed from the class diagram in design 03 section 3, which that
/// document declares its own implementer source of record. Neither is annotated nullable
/// there, and the diagram annotates nullable members explicitly elsewhere in the same
/// block, so both are non-nullable by statement.
/// </para>
/// <para>
/// The chain is <c>RecordHash = HMAC_k(PrevHash || canonical(fields))</c>. Three parts of
/// that are load-bearing. It is keyed rather than a bare digest, so an attacker who can
/// write to the store still cannot recompute a valid chain. The operands are prev-first,
/// which is the convention an independent verifier reproduces. And the fields are the
/// canonical TEXT rendering (ADR-0008).
/// </para>
/// </remarks>
public sealed class AuditChainEntry
{
    /// <summary>
    /// The previous record's hash. At genesis this is 32 zero bytes, not a string.
    /// </summary>
    public required byte[] PrevHash { get; init; }

    /// <summary>
    /// This record's hash, the keyed HMAC over the previous hash followed by the canonical
    /// record.
    /// </summary>
    public required byte[] RecordHash { get; init; }
}
