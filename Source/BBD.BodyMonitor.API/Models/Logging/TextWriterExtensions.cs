// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Provides extension methods for <see cref="TextWriter"/> to support writing messages with ANSI colors.
    /// This class is intended for internal use within the application.
    /// </summary>
    internal static class TextWriterExtensions
    {
        /// <summary>
        /// Writes a message to the <see cref="TextWriter"/> with specified background and foreground colors using ANSI escape codes.
        /// The colors are reset to default after the message is written.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="message">The message string to write.</param>
        /// <param name="background">The <see cref="ConsoleColor"/> to use for the background. If null, background color is not changed.</param>
        /// <param name="foreground">The <see cref="ConsoleColor"/> to use for the foreground. If null, foreground color is not changed.</param>
        public static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
            if (background.HasValue)
            {
                textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));
            }
            if (foreground.HasValue)
            {
                textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));
            }
            textWriter.Write(message);
            if (foreground.HasValue)
            {
                textWriter.Write(AnsiParser.DefaultForegroundColor); // reset to default foreground color
            }
            if (background.HasValue)
            {
                textWriter.Write(AnsiParser.DefaultBackgroundColor); // reset to the background color
            }
        }
    }
}
