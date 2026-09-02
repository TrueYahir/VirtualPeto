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
- Fixed an error that caused the pixel calculation in the sprite sheet to work incorrectly when creating a smart pet.
- Fixed an error when creating a GifPackage (path of the name was not found).
- Added option to edit GIF Packages.
- The application no longer closes due to erros.

### Planned
- **Advanced Pet Interactions:** Future update will include new behavioral options for smart pets, allowing them to interact directly with other active programs and windows on the desktop.
- **Removal background from Videos:** Future update will include the posibility to remove background from Videos.

## [2.2.3] - 2026-07-13
### Fixed
- Fixed an error that doesn't allow to close pets.

### Added
- Added a context menu for Pets & Smarts Pets, it can be used with right click.
- Added Objects funcionality (Objects added in program will be added in next update.)

### Planned
- **Add Food and Toys to smart pets** Future update wull include new functions as interactions of pets with food, and toys, that the user can add to the desktop.

## [2.2.3] - 2026-07-24
### Added
- Added favorite options, and filters for favorites.
- Added the option to initialize pets when starting the program.

## [2.2.4] - 2026-07-26
### Fixed
- Fixed a problem when creating a smart pet, it was not displayed in the list of smart pets.

### Added
- Items

## [2.2.5] - 2026-08-01
### Changed 
- Changes to the object creation interface.
- Changes in the way GIF are generated. 

### Added
- Now you can choose specific colors to delete in the GIF remover window.
- Now you can create tools/foods in the same window.

### Planned
- The next version will be the last update before the legacy version.

## [2.3.0] - 2026-08-13
### Added
- Video BG Remover added (Remove background of videos with a specific color)

## [2.3.1] - 2026-08-14
### Changed
- Changed visual elements in the main window.
- Optimized the way video-generated gifs play.
- Added the option to create a GIF Package from the Video Background Remover window.
- Added an option to size the pet when its running.
- Now you can see the type of archive in the library section.
- The food/object creator has been removed in the create pet section, the option in create smart pet has been added, as a pet object and extra animations for the pet reacting to the object.

### Added
- Added a optión to refresh the values of general settings for Smart pet values for all the archives. 
- Sprite Extractor 
- Audio Converter
- Food and items activated.
- Jukebox added to the program, the pet reacts to music. Jukebox specifications: play music, pause, mute, volume, playlist.
- Sprite extractor (extract sprites form a gif)
- Sprite unpacker (Gets the sprite from a sprite sheet, allows to create a new sprite sheet from specific sprites).
- Sprite packer

## [2.4] - 2026-09-02
### Fixed
- Fixed the following mouse animations