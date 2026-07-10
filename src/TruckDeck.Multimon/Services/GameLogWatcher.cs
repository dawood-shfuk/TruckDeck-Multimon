using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Watches game.log.txt for world load and window resize events.
    /// </summary>
    public sealed class GameLogWatcher
    {
        static readonly Regex WindowedSizePattern = new Regex(
            @"new windowed size:\s*(\d+)\s+(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly string _logPath;
        long _lastPosition;

        public GameLogWatcher(string logPath)
        {
            _logPath = logPath;
            Reset();
        }

        public void Reset()
        {
            _lastPosition = 0;
            if (!File.Exists(_logPath))
                return;

            try
            {
                using (var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    _lastPosition = stream.Length;
            }
            catch
            {
                _lastPosition = 0;
            }
        }

        public bool TryDetectWorldEntry()
        {
            return TryReadNewLines(line => IsWorldEntryLine(line) ? true : (bool?)null);
        }

        /// <summary>
        /// Detects when the game expands its window beyond the primary monitor (profile load).
        /// </summary>
        public bool TryDetectWindowResizeBeyondPrimary(Rectangle primaryBounds, out int width, out int height)
        {
            width = 0;
            height = 0;
            var detectedW = 0;
            var detectedH = 0;

            var found = TryReadNewLines(line =>
            {
                var match = WindowedSizePattern.Match(line);
                if (!match.Success)
                    return null;

                var w = int.Parse(match.Groups[1].Value);
                var h = int.Parse(match.Groups[2].Value);
                if (w <= primaryBounds.Width + 24 && h <= primaryBounds.Height + 24)
                    return null;

                detectedW = w;
                detectedH = h;
                return true;
            });

            if (found)
            {
                width = detectedW;
                height = detectedH;
            }

            return found;
        }

        bool TryReadNewLines(Func<string, bool?> predicate)
        {
            if (string.IsNullOrWhiteSpace(_logPath) || !File.Exists(_logPath))
                return false;

            try
            {
                using (var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (_lastPosition > stream.Length)
                        _lastPosition = 0;

                    stream.Seek(_lastPosition, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var result = predicate(line);
                            if (result == true)
                                return true;
                        }
                    }

                    _lastPosition = stream.Length;
                }
            }
            catch
            {
                // log may be locked briefly during write
            }

            return false;
        }

        public static bool IsWorldEntryLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.TrimEnd();
            return trimmed.EndsWith(": game", StringComparison.Ordinal)
                   || trimmed.Contains(" : game");
        }
    }
}
