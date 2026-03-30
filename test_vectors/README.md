Test vectors directory

Purpose
- Store test key material, PAN/PIN vectors and helper samples for integration tests.
- All values here are for development and testing only and MUST NOT be used in production.

Files
- `keys.yaml` — sample LMK and working keys (ZPK/TPK/TEK/PVK), DUKPT BDK/KSN placeholders and KCVs.
- `pan_pin_vectors.yaml` — PAN/PIN/PVV and sample encrypted PIN-block placeholders for test flows.

How to use
1. Replace placeholder tokens with real encrypted key tokens or clear test keys as required by your HSM test setup.
2. Keep expected PVV/KCV values updated after a one-time run against a trusted HSM or reference implementation.
3. Load vectors in NUnit integration tests (Yaml.NET or System.Text.Json with conversion) and drive HSM commands.

Security
- These files must live in the repo only for automated CI tests in safe test environments.
- Do NOT commit real production keys. Use secrets or CI-protected variables for sensitive artifacts.

Next steps
- Implement a small loader utility in `ThalesCore.Tests` to read `test_vectors/*.yaml` and provide strongly-typed objects to integration tests.
- Implement DUKPT helper utilities for deriving session keys from BDK+KSN.
