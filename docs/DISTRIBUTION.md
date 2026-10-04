# Public distribution audit — 2026-10-05

Included: independent C#/HTML/CSS/JS/SVG, gray theme/font-name preset, original gray terminal icon, build/test/packaging scripts, docs and complete third-party notices. Portable binary includes only project executables and permitted WebView2 SDK interface DLLs; .NET/WebView2 Runtime are not bundled.

Excluded: local music and BGM-bearing previews, modified GPT/OpenAI-knot icons, extracted official icon, reference original animation source/audit archives, user screenshots, login data, package snapshots, paths identifying a user/machine, private theme exports, personal shortcut files/backups, runtime logs/cache and mock test binaries. No font files are shipped.

OpenAI guidelines require logo use as supplied and disallow modifying it or using it as primary branding. Consequently the public icon is a new neutral `>_` terminal glyph. The user's private gray GPT icon stays in the private local project, outside all public archives.

The public code/asset license is MIT, excluding third-party components and marks. Upstream original-animation reuse permission remains unresolved; independent implementation does not imply endorsement or a license to the original. Public package is a preview candidate, with remaining actual-device checks documented in QA.

Repository packaging uses explicit top-level source allowlists and denied private/media/font extensions, independent of .gitignore. Runtime packaging has a separate payload allowlist. Verify manifests/checksums before attaching binaries to GitHub Releases. Runtime logs are never release material. Source repository is uploaded from the clean folder, not the private development project root.
