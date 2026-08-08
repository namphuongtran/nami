using Microsoft.Extensions.Options;

namespace Nami.Identity.Core;

/// <summary>
/// Rejects a <see cref="NamiIdentityOptions"/> that is missing a required value.
/// </summary>
/// <remarks>
/// <para>
/// This is the mechanism behind ADR-0096 parameter C, and it is what makes
/// parameter B safe. Design 01 section 5.3 requires that "a missing value
/// crashes the host at boot rather than surfacing lazily on the first request
/// that needs it", and the <c>required</c> modifier cannot deliver that here,
/// because the options system builds the instance by reflection rather than
/// through an object initializer.
/// </para>
/// <para>
/// <b>Both failures are reported, not the first one.</b> An operator who has
/// forgotten two values should learn both at the first boot rather than at the
/// second.
/// </para>
/// </remarks>
internal sealed class NamiIdentityOptionsValidator : IValidateOptions<NamiIdentityOptions>
{
    public ValidateOptionsResult Validate(string? name, NamiIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add($"{nameof(NamiIdentityOptions.ConnectionString)} is required and was not supplied.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{nameof(NamiIdentityOptions.Issuer)} is required and was not supplied.");
        }

        return failures.Count is 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
