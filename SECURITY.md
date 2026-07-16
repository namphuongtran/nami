# Security Policy

Nami is an identity provider: security reports are taken seriously and handled with priority.

## Reporting a vulnerability

**Do not open a public issue for security vulnerabilities.**

Report privately via [GitHub Security Advisories](https://github.com/namphuongtran/nami/security/advisories/new) ("Report a vulnerability" on the Security tab).

Please include: affected component/version (or commit), reproduction steps or proof of concept, and impact assessment if you have one.

## What to expect

- **Acknowledgement** within 72 hours.
- **Assessment and triage** within 7 days: we confirm the issue, assess severity, and agree on a disclosure timeline with you.
- **Coordinated disclosure**: we ask for up to 90 days to ship a fix before public disclosure. Credit is given to reporters in the advisory unless you prefer to stay anonymous.

## Scope

In scope: the Nami source code in this repository, published Nami packages, and the reference container image.

Out of scope: vulnerabilities exclusively in third-party dependencies (report upstream, but feel free to notify us so we can pin/patch), and issues requiring physical access or already-compromised hosts.

## Supported versions

Pre-1.0, only the latest release receives security fixes. A formal support matrix will be published with 1.0.
