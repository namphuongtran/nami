namespace Nami.Identity.Abstractions;

/// <summary>
/// The business audit trail. Records what was done: a client provisioned, consent granted,
/// a role assigned, a key rotated.
/// </summary>
/// <remarks>
/// <para>
/// <b>This port is security-sensitive, and its invariant binds any replacement.</b>
/// ADR-0075 section A states it: records are tamper-evident and delivery-guaranteed, they
/// are hash-chained, they reach their destination at least once, and the audit lane never
/// degrades into the diagnostics lane, which has neither property. That ADR calls the
/// invariant part of this contract in the same way the signature is, and a consumer
/// supplying an adapter verifies it by running the contract test rather than by reading a
/// paragraph.
/// </para>
/// <para>
/// <b>An implementation may not swallow an exception</b> (design 03 section 3). Three
/// shapes are forbidden by name, and each has been seen in the wild: using the logger as
/// the audit trail, fire-and-forget, and auditing after the business transaction commits
/// with no outbox and no retry. The third is the subtle one, because it passes every
/// happy-path test and loses exactly the records written when something was already going
/// wrong.
/// </para>
/// <para>
/// The signature is transcribed from the class diagram in design 03 section 3. The
/// parameter names are this repository's, following the Microsoft naming conventions that
/// ADR-0065 adopts by reference. The cancellation token is deliberately not optional, so
/// that a caller on the fail-closed critical path has to supply one.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// AuditChainEntry entry = await sink.AppendAsync(
///     new AuditEvent
///     {
///         EventType = "consent_grant",
///         ActorSubCiphertext = actorCiphertext,
///         TargetTenantId = tenantId,
///         PayloadCanonical = canonicalPayload,
///         CorrelationId = correlationId,
///         SubjectRef = subjectRef,
///         ClientId = clientId,
///     },
///     cancellationToken);
/// </code>
/// </example>
public interface IAuditSink
{
    /// <summary>
    /// Appends one hash-chained record and returns the entry it produced.
    /// </summary>
    /// <param name="auditEvent">The record to append.</param>
    /// <param name="cancellationToken">Cancels the append.</param>
    /// <returns>
    /// The new chain entry. Returning it is what lets a caller assert the append actually
    /// happened rather than assuming it.
    /// </returns>
    ValueTask<AuditChainEntry> AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
