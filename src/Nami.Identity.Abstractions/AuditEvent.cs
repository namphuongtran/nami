namespace Nami.Identity.Abstractions;

/// <summary>
/// One record on the business audit trail: a client provisioned, consent granted, a role
/// assigned, a key rotated.
/// </summary>
/// <remarks>
/// <para>
/// The eight members and their nullability are transcribed from the class diagram in
/// design 03 section 3, which that document declares its own implementer source of
/// record. The diagram annotates nullable members explicitly, so an unannotated member
/// here is non-nullable by statement rather than by assumption.
/// </para>
/// <para>
/// <see cref="SubjectRef"/>, <see cref="SourceIpHash"/>, and <see cref="ClientId"/> are the
/// ADR-0082 grouping keys, and they are in the canonical hashed field set from genesis.
/// Adding one later would be a chain schema version rather than an ordinary migration,
/// because a chain written under one field set cannot be verified under another.
/// </para>
/// </remarks>
public sealed class AuditEvent
{
    /// <summary>
    /// The catalogued event name, such as <c>consent_grant</c>, <c>token_issued</c>, or
    /// <c>key_rotation</c>.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The acting subject, as ciphertext at write time. Destroying the per-subject key
    /// makes it unreadable and leaves the record hash stable, which is what lets an
    /// erasure reach an append-only row (ADR-0016).
    /// </summary>
    public required string ActorSubCiphertext { get; init; }

    /// <summary>
    /// The deterministic subject surrogate that per-user grouping uses.
    /// <see cref="ActorSubCiphertext"/> cannot serve, because two events for one person
    /// need not share a value under the crypto-shred default. This is the same surrogate
    /// the processing-restriction table and the per-subject key vault use, so an erasure
    /// still has exactly one mapping to destroy (ADR-0082, ADR-0016).
    /// </summary>
    public Guid? SubjectRef { get; init; }

    /// <summary>
    /// A keyed HMAC-SHA256 of the source address, and deliberately not truncated: a
    /// collision in an abuse rule is false attribution, which is worse than none. It is a
    /// pseudonym and not anonymisation, so the protection is key custody plus access
    /// control rather than the hash (ADR-0082).
    /// </summary>
    public byte[]? SourceIpHash { get; init; }

    /// <summary>
    /// The registered application identifier. It is not personal data, so it is stored
    /// plainly. It stays off the metric lane regardless, because it is unbounded there.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// The tenant the event is about, captured at emission rather than at forward time.
    /// A missing tag fails the write rather than defaulting to a wrong tenant.
    /// </summary>
    public required Guid TargetTenantId { get; init; }

    /// <summary>
    /// The canonical TEXT rendering of the payload, which is what the chain hashes. It is
    /// not the stored <c>jsonb</c>, because PostgreSQL does not preserve that column's
    /// input byte order and the hash would then depend on the database's representation.
    /// </summary>
    public required string PayloadCanonical { get; init; }

    /// <summary>
    /// The correlation id. It is the only thing that joins the audit lane to the
    /// diagnostics lane, which the audit lane never routes through (ADR-0022).
    /// </summary>
    public required Guid CorrelationId { get; init; }
}
