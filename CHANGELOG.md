# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- **Advanced Pet Interactions:** Future update will include new behavioral options for smart pets, allowing them to interact directly with other active programs and windows on the desktop.

## [1.1.0] - 2026-06-23

### Added
- **Sprite Sheet Cutter:** Automated tool to extract individual frames from a grid and compile them into a looping GIF.
- **GIF Creator:** Tool to bind multiple static frames into a single animated GIF with custom framerate support.
- **Background Remover:** Utility to clear solid backgrounds from standard image files.
- **Magick.NET Integration:** Implemented robust image processing for reliable transparency and GIF metadata handling.
- **Smart Pets Library:** Core system to organize, preview, and launch pets to the desktop.

### Changed
- Refactored the UI layout to move image processing tools from the "Create Pet" tab to a dedicated "Tools" tab for better logical flow.

### Fixed
- Resolved an overlapping frame issue caused by the native WPF GIF encoder by switching to Magick.NET.
- Fixed an exception related to incorrect data types when assigning metadata (Delay and Disposal methods) to GIF frames.



## [1.2.1] - 2026-07-01

### Added
- Added background removal support for GIF files.
- Added a button to switch the library view.
- Added an option to launch the application automatically when Windows starts.
- Added an option to automatically clear memory usage.
- Added the ability to export user preference settings.
- Added an option to limit the maximum number of pets that can be opened simultaneously.
- Added support for GIF Packages, allowing GIFs to include custom sound effects.
- Added an option to lock pet interactions, preventing mouse clicks from affecting them.

### Changed
- Improved the background removal feature for images.
- Pet windows are no longer displayed in the Windows taskbar.
- Closing the main window now minimizes the application to the system tray while at least one pet is still active.

### Planned
- **Advanced Pet Interactions:** Future update will include new behavioral options for smart pets, allowing them to interact directly with other active programs and windows on the desktop.
- **Removal background from Videos:** Future update will include the posibility to remove background from Videos.