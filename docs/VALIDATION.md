# Local candidate validation — 2026-10-05

Version 0.1.0-preview.1. Existing Windows x64 .NET Framework compiler and pinned WebView2 SDK interface used. Both production entries and the separate mock harness compile. No SDK/runtime/system environment was installed or changed. GitHub CI itself has not been run.

## Checked on the development machine

| Check | Outcome | Boundary |
| --- | --- | --- |
| Actual Shell/package registration discovery | PASS | Read-only `--audit-client`; detects application/process name from actual registered metadata; no activation |
| Mock fresh, slow, skip, already, missing, timeout, retry, close | 8/8 PASS | Own mock target only; each activation-success target is the test process; no real foreground/cold-start guarantee |
| Standalone Full | PASS | 16000.2542ms animation, configured 450ms fade; sampled total exit 17.70s; no residual tracked processes |
| Standalone buttons/window controls, Esc, click | 3/3 PASS | Own DOM/internal test dispatch, not human physical-key/mouse acceptance |
| Whole-tree Full memory | 625.77 MiB sampled peak | Offscreen preview on this machine; not a universal maximum or low-memory claim |
| Frame timing | 954 frames, worst gap 201.2ms | Offscreen run; not a guarantee of uninterrupted display smoothness |
| Native first paint / no white flash | Manual check pending | Offscreen run did not emit a meaningful native-first-paint measurement |
| Original public terminal icon | PASS | 16/24/32/48/64/96/128/256px exports, alpha outside tile, locally reviewed |
| Public preview image | PASS | Direct screenshot of this candidate's own local WebView content; no official/private app screenshot |

Raw local logs, process IDs and machine paths remain private and are not package contents. An initial strict process-set comparison flagged concurrent official-process turnover; the mock run still completed its animation/reveal/disposal. The final eight-case run passed and showed no official process-set change. Tests prove the isolated bridge targets its own mock process, rather than treating unrelated process-set changes as proof of a client action.

The source/runtime archives are built from explicit allowlists, with denied media/font/private-path/secret-marker checks. Each archive includes a SHA256 file manifest; archive checksums are provided separately. The source archive is intended to be extracted and rebuilt with only the pinned dependency package and existing compiler, not the personal development directory.

Clean-source reconstruction completed locally: extracted the public source archive into a new directory, restored the pinned SDK interface from its SHA256-verified local package, compiled both production entries and the isolated harness, and ran the package allowlists again. All passed. Source has 42 indexed files; portable runtime has 27 indexed files, plus each archive's own manifest. Initial archive traversal and every indexed file size/hash were verified; binary and text scans found no development username, private project root or source-music marker. Rebuild success proves local reproducibility in this existing environment, not a test on another Windows device or a remotely executed CI run.

Still pending: human actual official cold start, real existing-client foreground permission, multiple displays/DPI, newer official package registration, actual theme import and public candidate playback approval. No remote GitHub repository/release was created or uploaded, and the existing private desktop launcher was not replaced.
