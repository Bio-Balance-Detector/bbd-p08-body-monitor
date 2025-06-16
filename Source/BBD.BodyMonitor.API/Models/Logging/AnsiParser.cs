// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Parses strings containing ANSI escape codes to extract text segments and their associated colors.
    /// This class is intended for internal use within the application, primarily for processing log messages with ANSI color codes.
    /// </summary>
    internal sealed class AnsiParser
    {
        private readonly Action<string, int, int, ConsoleColor?, ConsoleColor?> _onParseWrite;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiParser"/> class.
        /// </summary>
        /// <param name="onParseWrite">An action to be called when a segment of text is parsed.
        /// The action receives the original message string, the start index of the segment,
        /// the length of the segment, the parsed background color (if any), and the parsed foreground color (if any).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="onParseWrite"/> is null.</exception>
        public AnsiParser(Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite)
        {
            if (onParseWrite == null)
            {
                throw new ArgumentNullException(nameof(onParseWrite));
            }
            _onParseWrite = onParseWrite;
        }

        /// <summary>
        /// Parses a string message containing ANSI escape codes.
        /// It identifies text segments and their foreground/background colors based on a subset of SGR (Select Graphic Rendition) parameters.
        /// Supported SGR parameters include:
        /// <list type="bullet">
        ///   <item><description>0: Reset all attributes</description></item>
        ///   <item><description>1: Bright/Bold</description></item>
        ///   <item><description>Foreground Colors (30-37): Black, Red, Green, Yellow, Blue, Magenta, Cyan, White. Bright versions are applied if '1' was previously set.</description></item>
        ///   <item><description>39: Default foreground color</description></item>
        ///   <item><description>Background Colors (40-47): Black, Red, Green, Yellow, Blue, Magenta, Cyan, White.</description></item>
        ///   <item><description>49: Default background color</description></item>
        /// </list>
        /// The <paramref name="message"/> is processed, and the callback provided in the constructor (<see cref="_onParseWrite"/>) is invoked for each segment of text found between or after ANSI escape codes.
        /// </summary>
        /// <param name="message">The string message to parse.</param>
        public void Parse(string message)
        {
            int startIndex = -1;
            int length = 0;
            int escapeCode;
            ConsoleColor? foreground = null;
            ConsoleColor? background = null;
            var span = message.AsSpan();
            const char EscapeChar = '\x1B';
            ConsoleColor? color = null;
            bool isBright = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == EscapeChar && span.Length >= i + 4 && span[i + 1] == '[')
                {
                    if (span[i + 3] == 'm')
                    {
                        // Example: \x1B[1m
                        if (IsDigit(span[i + 2]))
                        {
                            escapeCode = (int)(span[i + 2] - '0');
                            if (startIndex != -1 && length > 0) // Only write if there's content
                            {
                                _onParseWrite(message, startIndex, length, background, foreground);
                            }
                            startIndex = -1; // Reset for next segment
                            length = 0;

                            if (escapeCode == 0) // Reset all attributes
                            {
                                foreground = null;
                                background = null;
                                isBright = false;
                            }
                            else if (escapeCode == 1) // Bright/Bold
                            {
                                isBright = true;
                            }
                            i += 3;
                            continue;
                        }
                    }
                    else if (span.Length >= i + 5 && span[i + 4] == 'm')
                    {
                        // Example: \x1B[40m or \x1B[31m
                        if (IsDigit(span[i + 2]) && IsDigit(span[i + 3]))
                        {
                            escapeCode = (int)(span[i + 2] - '0') * 10 + (int)(span[i + 3] - '0');
                            if (startIndex != -1 && length > 0) // Only write if there's content
                            {
                                _onParseWrite(message, startIndex, length, background, foreground);
                            }
                            startIndex = -1; // Reset for next segment
                            length = 0;

                            if (TryGetForegroundColor(escapeCode, isBright, out color))
                            {
                                foreground = color;
                                // Reset brightness after applying a foreground color,
                                // as brightness is typically applied per color code.
                                if (escapeCode != 39) isBright = false; // Don't reset for default color code
                            }
                            else if (TryGetBackgroundColor(escapeCode, out color))
                            {
                                background = color;
                            }
                            i += 4;
                            continue;
                        }
                    }
                }

                // Regular character processing
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                length++; // Increment length for the current character

            } // End of for loop

            // After loop, if there's any remaining text segment, write it
            if (startIndex != -1 && length > 0)
            {
                _onParseWrite(message, startIndex, length, background, foreground);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char c) => (uint)(c - '0') <= ('9' - '0');

        internal const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
        internal const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

        internal static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => "\x1B[30m",
                ConsoleColor.DarkRed => "\x1B[31m",
                ConsoleColor.DarkGreen => "\x1B[32m",
                ConsoleColor.DarkYellow => "\x1B[33m",
                ConsoleColor.DarkBlue => "\x1B[34m",
                ConsoleColor.DarkMagenta => "\x1B[35m",
                ConsoleColor.DarkCyan => "\x1B[36m",
                ConsoleColor.Gray => "\x1B[37m",
                ConsoleColor.Red => "\x1B[1m\x1B[31m", // Bright Red
                ConsoleColor.Green => "\x1B[1m\x1B[32m", // Bright Green
                ConsoleColor.Yellow => "\x1B[1m\x1B[33m", // Bright Yellow
                ConsoleColor.Blue => "\x1B[1m\x1B[34m", // Bright Blue
                ConsoleColor.Magenta => "\x1B[1m\x1B[35m", // Bright Magenta
                ConsoleColor.Cyan => "\x1B[1m\x1B[36m", // Bright Cyan
                ConsoleColor.White => "\x1B[1m\x1B[37m", // Bright White
                _ => DefaultForegroundColor // default foreground color
            };
        }

        internal static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            // For background colors, "bright" versions are not standard via SGR codes 40-47.
            // SGR codes 100-107 are for bright background colors but are less universally supported.
            // This implementation maps DarkGray to Gray for background for better visibility if needed.
            return color switch
            {
                ConsoleColor.Black => "\x1B[40m",
                ConsoleColor.DarkRed => "\x1B[41m",
                ConsoleColor.DarkGreen => "\x1B[42m",
                ConsoleColor.DarkYellow => "\x1B[43m",
                ConsoleColor.DarkBlue => "\x1B[44m",
                ConsoleColor.DarkMagenta => "\x1B[45m",
                ConsoleColor.DarkCyan => "\x1B[46m",
                ConsoleColor.Gray => "\x1B[47m", // Standard White background
                // ConsoleColor.DarkGray could map to "\x1B[100m" (Bright Black/Dark Gray) if needed
                // Other bright colors (Red, Green, etc.) don't have direct standard background SGR codes in the 40-47 range.
                _ => DefaultBackgroundColor // Use default background color
            };
        }

        private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
        {
            color = number switch
            {
                30 => ConsoleColor.Black, // Black
                31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed, // Red
                32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen, // Green
                33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow, // Yellow
                34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue, // Blue
                35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta, // Magenta
                36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan, // Cyan
                37 => isBright ? ConsoleColor.White : ConsoleColor.Gray, // White (bright) / Gray (normal)
                39 => ConsoleColor.Gray, // Default foreground color - behavior can vary; often Gray or White.
                _ => null
            };
            // Return true if a color was matched OR if it's the default color code (39)
            return color != null;
        }

        private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
        {
            color = number switch
            {
                40 => ConsoleColor.Black,
                41 => ConsoleColor.DarkRed,
                42 => ConsoleColor.DarkGreen,
                43 => ConsoleColor.DarkYellow,
                44 => ConsoleColor.DarkBlue,
                45 => ConsoleColor.DarkMagenta,
                46 => ConsoleColor.DarkCyan,
                47 => ConsoleColor.Gray, // Standard White background
                49 => ConsoleColor.Black, // Default background color - behavior can vary; often Black.
                _ => null
            };
            // Return true if a color was matched OR if it's the default color code (49)
            return color != null;
        }
    }
}