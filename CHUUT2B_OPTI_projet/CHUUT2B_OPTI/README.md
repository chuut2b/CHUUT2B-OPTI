# CHUUT2B OPTI

A native WPF/.NET 8 Windows application for broad gaming-PC auditing.

## What it does
- CPU, motherboard and BIOS inventory
- RAM capacity and configured speed
- GPU, driver and display mode
- PnP device errors
- disk free space
- active power plan
- active network interfaces
- startup entries
- background memory pressure snapshot
- HAGS/Game DVR registry state
- gaming-oriented score from 0-100
- exportable report

## Safety model
The audit is read-only. The optimizer button is intentionally conservative: BIOS, firmware, overclocking, audio, controller and driver changes are not applied automatically.

## Build
On Windows with .NET 8 SDK:
1. Open this folder.
2. Run `BUILD_WINDOWS.cmd`.
3. The single-file executable is created at `publish\CHUUT2B_OPTI.exe`.

No SDK? Push the folder to GitHub and run the "Build EXE" workflow (Actions tab), then download the artifact.

The project requests Administrator rights so hardware/driver visibility is broader.
