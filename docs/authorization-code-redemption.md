# Make authorization codes single-use in IdentityServer

## Finding from the RDAO Service Portal release review

An authorization code gives a client one opportunity to exchange it for tokens. The shared IdentityServer core enables OpenIddict's authorization-code flow in degraded mode. Its handlers validate the request and the client. The review found no handler or store that records successful consumption of the code. A second otherwise valid redemption within the code's lifetime could therefore issue another token set. This is a source-review finding. Duplicate issuance has not been observed.

The standalone issuer and the embedded Umbraco issuer both use this registration. The embedded host adds exact callback checks and requires PKCE. PKCE binds redemption to the client holding the verifier. Recording consumption enforces the separate single-use requirement. The core lifetime setting limits the exposure window. Regenerating signing keys during recovery serves a different operational purpose. [Shared core registration](../IdentityServer.Core/IdentityServer.cs) · [Embedded host registration](../../AurorisNet.Umbraco.IdentityServer.SSO/src/Embedded/EmbeddedIdentityServerHost.cs)

OpenIddict 7.9.0 filters its built-in `RedeemTokenEntry` handler on degraded mode being disabled and token storage being enabled. OAuth requires denial of a repeated exchange and recommends revoking tokens previously issued from that code when possible. [OpenIddict handler](https://github.com/openiddict/openiddict-core/blob/7.9.0/src/OpenIddict.Server/OpenIddictServerHandlers.cs#L2685-L2709) · [RFC 6749 §4.1.2](https://www.rfc-editor.org/rfc/rfc6749#section-4.1.2)

## Work for the IdentityServer chat

Evaluate OpenIddict's standard token storage and redemption handling first. Identify the client-validation changes and storage requirements for standalone and embedded installations. The embedded adapter replaces specific core handlers and checks their count at startup. If a lightweight store is needed, specify how the issuer atomically consumes a code before issuing tokens, shares that decision across workers serving the same issuer, and expires records once the code can no longer be accepted.

## Acceptance criteria

- A valid sign-in succeeds in each host. At most one redemption succeeds for the same grant, including concurrent decisions across supported workers.
- Restarting a worker while a previously consumed code is still valid cannot make it usable again. A storage failure prevents token issuance until the issuer can establish single use.
- Document what the issuer can revoke after duplicate use, how expiry and cleanup work, and any deployment prerequisites. Keep codes, verifiers and tokens out of logs.
- Existing callback, client authentication, PKCE and Windows sign-in rules still apply. The portal should receive the shared fix through its IdentityServer dependency.
