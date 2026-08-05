namespace Nami.Identity.Abstractions;

/// <summary>
/// One record on the security-event lane: a login failure, a token reject, a replay,
/// degraded mode enabled, break-glass.
/// </summary>
/// <remarks>
/// <para>
/// The eight members and their nullability are transcribed from the class diagram in
/// design 03 section 3, which that document declares its own implementer source of
/// record. The diagram annotates nullable members explicitly, so an unannotated member
/// here is non-nullable by statement rather than by assumption.
/// </para>
/// <para>
/// This type and <see cref="AuditEvent"/> are one lane split by responsibility, and design
/// 03 section 3 is where that split is stated. The lane is hash-chained and
/// delivery-guaranteed, and it is never the diagnostics lane, which has neither property
/// (ADR-0008, ADR-0022).
/// </para>
/// </remarks>
public sealed class SecurityEvent
{
    /// <summary>
    /// The catalogued event name, such as <c>login_failure</c>, <c>token_reject</c>, or
    /// <c>refresh_reuse_detected</c>.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The outcome. It is a field rather than part of <see cref="EventType"/>, so a query
    /// for every denial does not depend on parsing names.
    /// </summary>
    public required string Outcome { get; init; }

    /// <summary>
    /// The acting subject, as ciphertext at write time. Destroying the per-subject key
    /// makes it unreadable and leaves the record hash stable (ADR-0016).
    /// </summary>
    public required string ActorSubCiphertext { get; init; }

    /// <summary>
    /// The deterministic subject surrogate that per-user grouping uses.
    /// <see cref="ActorSubCiphertext"/> cannot serve, because two events for one person
    /// need not share a value under the crypto-shred default (ADR-0082).
    /// </summary>
    public Guid? SubjectRef { get; init; }

    /// <summary>
    /// A keyed HMAC-SHA256 of the source address, and deliberately not truncated. It is
    /// nullable and emission-configurable, because its data-protection basis is a pre-GA
    /// ratification item. Per-address detection does not depend on it (ADR-0082,
    /// ADR-0083).
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
    /// The correlation id. It is the only thing that joins this lane to the diagnostics
    /// lane, which this lane never routes through (ADR-0022).
    /// </summary>
    public required Guid CorrelationId { get; init; }
}
