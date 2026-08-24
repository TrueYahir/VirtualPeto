using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace VirtualPeto
{
    public static class PetPacker
    {
        public static void CreatePetPackage(PetMetadata metadata, string outputFilePath, string? originalVpetPath = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "VirtualPeto_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                if (!string.IsNullOrEmpty(originalVpetPath) && File.Exists(originalVpetPath))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(originalVpetPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName.Equals("config.json", StringComparison.OrdinalIgnoreCase)) continue;

                            string dest = Path.Combine(tempDir, entry.FullName);
                            entry.ExtractToFile(dest, true);
                        }
                    }
                }

                metadata.IdleAnimation.FilePath = ProcessFile(metadata.IdleAnimation.FilePath, tempDir, "idle");
                metadata.IdleAnimation.SoundPath = ProcessFile(metadata.IdleAnimation.SoundPath, tempDir, "idle_audio");

                metadata.SleepAnimation.FilePath = ProcessFile(metadata.SleepAnimation.FilePath, tempDir, "sleep");
                metadata.SleepAnimation.SoundPath = ProcessFile(metadata.SleepAnimation.SoundPath, tempDir, "sleep_audio");

                metadata.ClickedAnimation.FilePath = ProcessFile(metadata.ClickedAnimation.FilePath, tempDir, "clicked");
                metadata.ClickedAnimation.SoundPath = ProcessFile(metadata.ClickedAnimation.SoundPath, tempDir, "clicked_audio");

                metadata.DraggedAnimation.FilePath = ProcessFile(metadata.DraggedAnimation.FilePath, tempDir, "dragged");
                metadata.DraggedAnimation.SoundPath = ProcessFile(metadata.DraggedAnimation.SoundPath, tempDir, "dragged_audio");

                metadata.IntroAnimation.FilePath = ProcessFile(metadata.IntroAnimation.FilePath, tempDir, "intro");
                metadata.IntroAnimation.SoundPath = ProcessFile(metadata.IntroAnimation.SoundPath, tempDir, "intro_audio");

                metadata.OutroAnimation.FilePath = ProcessFile(metadata.OutroAnimation.FilePath, tempDir, "outro");
                metadata.OutroAnimation.SoundPath = ProcessFile(metadata.OutroAnimation.SoundPath, tempDir, "outro_audio");

                metadata.ListeningAnimation.FilePath = ProcessFile(metadata.ListeningAnimation.FilePath, tempDir, "listening");
                metadata.ListeningAnimation.SoundPath = ProcessFile(metadata.ListeningAnimation.SoundPath, tempDir, "listening_audio");

                metadata.NotificationAnimation.FilePath = ProcessFile(metadata.NotificationAnimation.FilePath, tempDir, "notification");
                metadata.NotificationAnimation.SoundPath = ProcessFile(metadata.NotificationAnimation.SoundPath, tempDir, "notification_audio");

                metadata.WakeUpAnimation.FilePath = ProcessFile(metadata.WakeUpAnimation.FilePath, tempDir, "wakeup");
                metadata.WakeUpAnimation.SoundPath = ProcessFile(metadata.WakeUpAnimation.SoundPath, tempDir, "wakeup_audio");

                var keys = new System.Collections.Generic.List<string>(metadata.Movements.Keys);
                foreach (var key in keys)
                {
                    metadata.Movements[key].FilePath = ProcessFile(metadata.Movements[key].FilePath, tempDir, $"move_{key}");
                    metadata.Movements[key].SoundPath = ProcessFile(metadata.Movements[key].SoundPath, tempDir, $"move_{key}_audio");
                }

                if (metadata.RandomActions != null)
                {
                    for (int i = 0; i < metadata.RandomActions.Count; i++)
                    {
                        if (metadata.RandomActions[i].Animation != null)
                        {
                            metadata.RandomActions[i].Animation.FilePath = ProcessFile(
                                metadata.RandomActions[i].Animation.FilePath, 
                                tempDir, 
                                $"random_{i}"
                            );
                            metadata.RandomActions[i].Animation.SoundPath = ProcessFile(
                                metadata.RandomActions[i].Animation.SoundPath, 
                                tempDir, 
                                $"random_{i}_audio"
                            );
                        }
                    }
                }

                string jsonString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(tempDir, "config.json"), jsonString);
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                ZipFile.CreateFromDirectory(tempDir, outputFilePath);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static string ProcessFile(string? inputPath, string tempDir, string baseName)
        {
            if (string.IsNullOrWhiteSpace(inputPath)) return string.Empty;
            if (Path.IsPathRooted(inputPath) && File.Exists(inputPath))
            {
                string ext = Path.GetExtension(inputPath);
                string newFileName = baseName + ext;
                File.Copy(inputPath, Path.Combine(tempDir, newFileName), true);
                return newFileName;
            }
            string existingPath = Path.Combine(tempDir, Path.GetFileName(inputPath));
            if (File.Exists(existingPath))
            {
                return Path.GetFileName(inputPath);
            }
            return string.Empty;
        }
    }
}