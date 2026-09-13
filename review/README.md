# Monkey Mouse development review

The JSON fragment and four-screen measurements template are incomplete, not runnable configurations. They intentionally contain no credentials or measured personal display inventory. `config-check` validates the four roles and emits routing settings; it does not install configuration, establish a network session or apply a display layout.

Use the review tool's `displays --host Mac|ASUS` output for engine IDs and coordinates. IDs depend on enumeration and monitor count; recollect all displays after connecting or rearranging a monitor. `EngineDefaultMouseScale` is the engine's default mouse multiplier, not an OS DPI measurement. Record and validate the intended engine scale separately. Do not mix physical pixels with logical coordinates. The builder validates geometry/scale presence but its output contains route settings, not a complete applied per-screen configuration.

The OS handles movement between two local Mac displays. Route reversal is validated using synthetic geometry; it does not prove hardware behavior. Empty hotkey arrays are not a disable switch in the inherited parser; policy is enforced in InputRouter and the secure relay.

See PAIRING.md, NATIVE-STORAGE.md and VALIDATION.md. Startup remains blocked until the missing native/platform and hardware checks are resolved.
