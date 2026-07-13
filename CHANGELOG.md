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

## [1.2.2] - 2026-07-02

### Added
- Added an option to block pet movement.

### Fixed
- Fixed an issue that did not allow pets to be overlaid in full screen/borderless window applications.

## [2.0.0] - 2026-07-03

### Added
- Added 3D Smart Pets with new behavior logic and animations (walk, run, sleep, wake up).
- Added a new window for Smart Pet creation, supporting both Sprite Sheets and GIFs.
- Added support for Smart Pets to move and navigate across multiple monitors.
- Added collision detection, allowing pets to react to collisions.
- Added audio reactivity, allowing pets to react to system sound.
- Added notification reactivity, allowing pets to react to system notifications.

### Changed
- Optimized the main page for better performance.
- Updated and modified the functionality of the Tools section.
- Implemented general visual improvements throughout the user interface.
- Cleaned up and refactored `ConfigWindow.xaml.cs` for better code maintainability.

### Fixed
- Fixed internal logic errors in `AudioDetector.cs`.
- Fixed internal logic errors in `NotificationDetector.cs`.
- Fixed a bug where pets would slide across the screen while performing non-movement animations.
- Fixed an audio bug that caused pet sound effects to loop infinitely.

## [2.1.1] - 2026-07-08

### Added
- Added an option to block pet movement.

### Fixed
- Fixed an issue that did not allow pets to be overlaid in full screen/borderless window applications.

## [2.2.1] - 2026-07-12

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

## [2.2.2] - 2026-07-13
### Fixed
Fixed an error that caused the pixel calculation in the sprite sheet to work incorrectly when creating a smart pet.

### Planned
- **Advanced Pet Interactions:** Future update will include new behavioral options for smart pets, allowing them to interact directly with other active programs and windows on the desktop.
- **Removal background from Videos:** Future update will include the posibility to remove background from Videos.