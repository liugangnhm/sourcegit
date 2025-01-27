﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SourceGit.Models
{
    public class ExternalTool
    {
        public string Name { get; private set; }
        public string Executable { get; private set; }
        public string OpenCmdArgs { get; private set; }
        public Bitmap IconImage { get; private set; } = null;

        public ExternalTool(string name, string icon, string executable, string openCmdArgs)
        {
            Name = name;
            Executable = executable;
            OpenCmdArgs = openCmdArgs;

            try
            {
                var asset = AssetLoader.Open(new Uri($"avares://SourceGit/Resources/ExternalToolIcons/{icon}.png",
                    UriKind.RelativeOrAbsolute));
                IconImage = new Bitmap(asset);
            }
            catch
            {
                // ignore
            }
        }

        public void Open(string repo)
        {
            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = repo,
                FileName = Executable,
                Arguments = string.Format(OpenCmdArgs, repo),
                UseShellExecute = false,
            });
        }
    }

    public class JetBrainsState
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;
        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = string.Empty;
        [JsonPropertyName("tools")]
        public List<JetBrainsTool> Tools { get; set; } = new List<JetBrainsTool>();
    }

    public class JetBrainsTool
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }
        [JsonPropertyName("toolId")]
        public string ToolId { get; set; }
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; }
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("displayVersion")]
        public string DisplayVersion { get; set; }
        [JsonPropertyName("buildNumber")]
        public string BuildNumber { get; set; }
        [JsonPropertyName("installLocation")]
        public string InstallLocation { get; set; }
        [JsonPropertyName("launchCommand")]
        public string LaunchCommand { get; set; }
    }

    public class ExternalToolsFinder
    {
        public List<ExternalTool> Founded
        {
            get;
            private set;
        } = new List<ExternalTool>();

        public void TryAdd(string name, string icon, string args, string env, Func<string> finder)
        {
            var path = Environment.GetEnvironmentVariable(env);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = finder();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;
            }

            Founded.Add(new ExternalTool(name, icon, path, args));
        }

        public void VSCode(Func<string> platformFinder)
        {
            TryAdd("Visual Studio Code", "vscode", "\"{0}\"", "VSCODE_PATH", platformFinder);
        }

        public void VSCodeInsiders(Func<string> platformFinder)
        {
            TryAdd("Visual Studio Code - Insiders", "vscode_insiders", "\"{0}\"", "VSCODE_INSIDERS_PATH", platformFinder);
        }

        public void VSCodium(Func<string> platformFinder)
        {
            TryAdd("VSCodium", "codium", "\"{0}\"", "VSCODIUM_PATH", platformFinder);
        }

        public void Fleet(Func<string> platformFinder)
        {
            TryAdd("Fleet", "fleet", "\"{0}\"", "FLEET_PATH", platformFinder);
        }

        public void SublimeText(Func<string> platformFinder)
        {
            TryAdd("Sublime Text", "sublime_text", "\"{0}\"", "SUBLIME_TEXT_PATH", platformFinder);
        }

        public void FindJetBrainsFromToolbox(Func<string> platformFinder)
        {
            var exclude = new List<string> { "fleet", "dotmemory", "dottrace", "resharper-u", "androidstudio" };
            var supported_icons = new List<string> { "CL", "DB", "DL", "DS", "GO", "JB", "PC", "PS", "PY", "QA", "QD", "RD", "RM", "RR", "WRS", "WS" };
            var state = Path.Combine(platformFinder(), "state.json");
            if (File.Exists(state))
            {
                var stateData = JsonSerializer.Deserialize(File.ReadAllText(state), JsonCodeGen.Default.JetBrainsState);
                foreach (var tool in stateData.Tools)
                {
                    if (exclude.Contains(tool.ToolId.ToLowerInvariant()))
                        continue;

                    Founded.Add(new ExternalTool(
                        $"{tool.DisplayName} {tool.DisplayVersion}",
                        supported_icons.Contains(tool.ProductCode) ? $"JetBrains/{tool.ProductCode}" : "JetBrains/JB",
                        Path.Combine(tool.InstallLocation, tool.LaunchCommand),
                        "\"{0}\""));
                }
            }
        }
    }
}
