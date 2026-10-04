# QA and acceptance

Version: 0.1.0-preview.1. Public packaging combines gray independent Full visuals with the previously developed external controller and new registered-entry discovery; it is an isolated candidate, not an automatic replacement of the developer's private desktop runtime.

Automated candidate checks and their current outcomes are summarized in VALIDATION.md. They cover compilation, real read-only registration detection, isolated bridge lifecycle cases, standalone DOM controls/Esc/click, complete Full clock/fade/browser exit, safe public archive contents and reconstruction from the source archive. No official cold-start was performed as part of packaging.

Remaining human acceptance: fresh official start, existing-client foreground transfer, minimized/background client, interactive native skip, delayed window loading, official update behavior, multiple monitors/DPI and actual theme font import. Automated mock focus does not prove real foreground permission. GitHub CI builds packages but does not approve those behaviors.

Known limitation: high transient WebView2 memory. Historical private Full measurements using the same architecture were around 800–850 MiB for the whole process tree. Candidate measurements, when available, are listed separately in VALIDATION.md. Do not describe the launcher as low-memory or universally smooth. Processes should exit after fade; failure to exit is a bug, not a feature.

Public defaults contain no music. The optional local-audio interface was inherited from the previously tested personal launcher, but public archives do not supply or test third-party BGM. Test music separately with media you are permitted to use.
