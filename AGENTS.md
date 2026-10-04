# Public project guardrails

- Keep all official application files, WindowsApps/Store packages, signatures, update paths, credentials and original shortcuts untouched.
- No process injection, hooks, debugger attach, security-setting/registry/hosts/startup modifications or permanent services.
- Resolve registered launch targets from the actual machine; do not guess Microsoft Store paths.
- Runtime is local and offline; dependency restoration is a separate explicit build step. Never automatically install a large environment or runtime.
- Version outputs and preserve delivered artifacts. A successful build/test is not human playback approval.
- Public allowlists exclude personal paths/data/themes, logs/caches, reference originals, unlicensed music, proprietary font files and modified official logos.
- Fonts are system-name references. Music is optional user media, disabled by default. Public icon is the original terminal glyph.
- Do not upload/create a remote repository, release or messages unless directly authorized by the human.
- Keep evidence and build cache under build/; stop after a reviewed public candidate before changing any existing desktop deployment.
