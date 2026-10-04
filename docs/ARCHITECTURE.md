# Architecture

Two separately compiled entries share local boot resources and config. `550C-Boot-Preview.exe` contains the standalone host only, with no client discovery/activation/window bridge. `550C-CRT-Launcher.exe` contains public registered-app discovery and the external lifecycle controller.

Client discovery queries Shell AppsFolder metadata for ChatGPT, validates the audited official publisher identifier, resolves the registered package using GetPackagePathByFullName, and reads that actual manifest to determine application ID and executable process name. It never executes a discovered internal version path: IApplicationActivationManager requests the registered application ID. A unique verified registration is required; conflicting entries fail unless the user selects an actually detected appId in config.

Window identity is checked by actual package family/application ID/process image name using limited query permissions. Enumeration, foreground and restore APIs provide external readiness/reveal behavior. No injection, debugger, app patch or credential access. The readiness detector proves a visible main window, not that remote services or all chat content are fully loaded.

Full animation has a 16s clock, 450ms fade, 160ms skip and a natural final wait state. WebView initializes with a matching black native surface and native ribbon draw. CSS/JS waits preserve the final state until a target appears. Full text/phase data is configured in JSON. BGM is optional local user media and off by default.

Separate temporary WebView profiles are project-owned. Dispose browser, wait for exit, and delete only this run's generated cache after validating its absolute path is beneath runtime/. Logs remain available locally for diagnosis. No persistent service/port is created. Build-time NuGet restore is a separate explicit network step.
