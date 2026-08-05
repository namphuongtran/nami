namespace Nami.Identity.Abstractions;

/// <summary>
/// The security-event lane. Records a login failure, a token reject, a replay, degraded
/// mode enabled, break-glass.
/// </summary>
/// <remarks>
/// <para>
/// <b>This port is security-sensitive, and its invariant binds any replacement.</b>
/// ADR-0075 section A names it alongside <see cref="IAuditSink"/> under one invariant:
/// records are tamper-evident and delivery-guaranteed, they are hash-chained, they reach
/// their destination at least once, and the audit lane never degrades into the diagnostics
/// lane, which has neither property.
/// </para>
/// <para>
/// <b>An implementation may not swallow an exception</b> (design 03 section 3). Design 01
/// section 3.3 gives the purpose of the split from <see cref="IAuditSink"/>: it is
/// interface segregation, and what it segregates is the tamper-evident lane, which never
/// routes through the diagnostics pipeline. The two lanes are joined only by a correlation
/// id (ADR-0008, ADR-0022).
/// </para>
/// <para>
/// This member returns no chain entry, which is the one place the two ports differ beyond
/// their payload. The signature is transcribed from the class diagram in design 03
/// section 3. The parameter names are this repository's, following the Microsoft naming
/// conventions that ADR-0065 adopts by reference.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await sink.AppendAsync(
///     new SecurityEvent
///     {
///         EventType = "login_failure",
///         Outcome = "denied",
///         ActorSubCiphertext = actorCiphertext,
///         TargetTenantId = tenantId,
///         CorrelationId = correlationId,
///         SubjectRef = subjectRef,
///         SourceIpHash = sourceIpHash,
///     },
///     cancellationToken);
/// </code>
/// </example>
public interface ISecurityEventSink
{
    /// <summary>
    /// Appends one hash-chained security event.
    /// </summary>
    /// <param name="securityEvent">The record to append.</param>
    /// <param name="cancellationToken">Cancels the append.</param>
    ValueTask AppendAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
}
