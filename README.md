# VirtualPeto

VirtualPeto is a desktop application built with C# and WPF that allows users to create, manage, and display interactive "Smart Pets" directly on their screens. It also includes a powerful suite of built-in tools to help prepare animation assets without relying on third-party software.

---

## Features

*   **Smart Pets Manager:** Organize, preview, and launch virtual pets directly to the desktop screen.
*   **Media Library:** Manage imported images, GIFs, and videos efficiently.
*   **Pet Creator:** Generate the necessary folder structure and configurations for new smart pet templates.
*   **Desktop Integration:** 
        * Launch the application automatically when Windows starts.
        * Keep pets running in the background by minimizing the application to the system tray when the main windows is closed.
        * Hide pet windows from the Windows taskbar for a cleaner desktop experience.
*   **Pet Management:**
        * Configure the maximum number of active pets.
        * Lock pet interaction to prevent accidental clicks or dragging.
        * Export user preferences and settings for backup or migration.
        * Automatically clear unused memory to improve long-running performance.
*   **Built-in Toolkit:**
    *   **Sprite Sheet Cutter:** Slice a grid-based sprite sheet into individual frames and generate a perfectly looping, transparent GIF.
    *   **GIF Creator:** Select multiple static images (PNG/JPG) and bind them together into a custom animated GIF with adjustable framerates.
    *   **Background Remover:** Remove solid background colors from images based on the top-left pixel color.

## Technologies Used

*   **C# / .NET**
*   **WPF (Windows Presentation Foundation)** for the graphical user interface.
*   **Magick.NET** for advanced image processing, transparent GIF handling, and loop generation.
*   **WpfAnimatedGif** for smooth GIF rendering within the application.

---

## Getting Started

### Prerequisites
Ensure the .NET SDK is installed on your development machine.
**[Download .NET SDK](https://dotnet.microsoft.com/es-es/download)**

## Download Latest Version

**[Download VirtualPeto v1.2-beta for Windows (64-bit)](https://github.com/TrueYahir/VirtualPeto/releases/tag/v1.2-beta)**

*Extract the .zip file and run `VirtualPeto.exe` to start the application. No installation is required.*

