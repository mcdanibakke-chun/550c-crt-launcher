# Contributing

Keep the launcher external, local and reversible. Do not patch the official application, alter security settings, add startup registration, inject into processes or replace original shortcuts. Do not add music, fonts, third-party logos, reference originals or private logs without explicit redistribution rights and provenance.

Use a fresh versioned output folder, build with the fixed SDK package, run isolated tests, and mark human playback separately from automated QA. New tests must target lifecycle behavior or real regressions; do not add redundant tests for cosmetic reversible edits.

Changes should explain the actual trigger/result, validation and remaining platform limitations. Before a public release, verify source/runtime archives using scripts/package.ps1. Actual official-client focus/cold start and visual/font approval are manual release checks.
