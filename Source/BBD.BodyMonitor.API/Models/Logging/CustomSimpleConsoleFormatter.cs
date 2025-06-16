// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Console;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Formats log messages for console output with custom simple formatting rules.
    /// This class is sealed and intended for internal use within the application.
    /// It handles log levels, timestamps, scopes, and colorization of output.
    /// </summary>
    internal sealed class CustomSimpleConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = System.Environment.NewLine + _messagePadding;
#if NETCOREAPP
        private static bool IsAndroidOrAppleMobile => OperatingSystem.IsAndroid() ||
                                                      OperatingSystem.IsTvOS() ||
                                                      OperatingSystem.IsIOS(); // returns true on MacCatalyst
#else
        private static bool IsAndroidOrAppleMobile => false;
#endif
        private IDisposable? _optionsReloadToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSimpleConsoleFormatter"/> class.
        /// </summary>
        /// <param name="options">An <see cref="IOptionsMonitor{SimpleConsoleFormatterOptions}"/> used to monitor and reload formatter options.</param>
        public CustomSimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base("customSimpleConsoleFormatter") // Name of the formatter
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        [MemberNotNull(nameof(FormatterOptions))]
        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        /// <summary>
        /// Releases the resources used by the <see cref="CustomSimpleConsoleFormatter"/>.
        /// Specifically, it disposes of the options reload token.
        /// </summary>
        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        /// <summary>
        /// Gets or sets the options for this console formatter.
        /// </summary>
        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

        /// <summary>
        /// Writes the log message to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <typeparam name="TState">The type of the object representing the state.</typeparam>
        /// <param name="logEntry">The log entry to write.</param>
        /// <param name="scopeProvider">An <see cref="IExternalScopeProvider"/> to obtain scope information, if any.</param>
        /// <param name="textWriter">The <see cref="TextWriter"/> to write the formatted log message to.</param>
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string? timestamp = null;
            string? timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTimeOffset dateTimeOffset = GetCurrentDateTime();
                timestamp = dateTimeOffset.ToString(timestampFormat);
            }
            if (timestamp != null)
            {
                textWriter.Write(timestamp);
            }
            if (logLevelString != null)
            {
                // Custom extension method WriteColoredMessage is assumed to be defined elsewhere,
                // for example, on TextWriterExtensions.
                textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);
            }
            CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider);
        }

        private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider? scopeProvider)
        {
            bool singleLine = FormatterOptions.SingleLine;
            //int eventId = logEntry.EventId.Id; // EventId is not used in this formatter's output
            Exception? exception = logEntry.Exception;

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id (though EventId is not written here)
            textWriter.Write(LoglevelPadding);
            // No category writing in this version, but could be: textWriter.Write(logEntry.Category);
            if (!singleLine)
            {
                textWriter.Write(System.Environment.NewLine);
            }

            // scope information
            WriteScopeInformation(textWriter, scopeProvider, singleLine);
            WriteMessage(textWriter, message, singleLine);

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                WriteMessage(textWriter, exception.ToString(), singleLine);
            }
            if (singleLine)
            {
                textWriter.Write(System.Environment.NewLine);
            }
        }

        private static void WriteMessage(TextWriter textWriter, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (singleLine)
                {
                    textWriter.Write(' ');
                    WriteReplacing(textWriter, System.Environment.NewLine, " ", message);
                }
                else
                {
                    textWriter.Write(_messagePadding);
                    WriteReplacing(textWriter, System.Environment.NewLine, _newLineWithMessagePadding, message);
                    textWriter.Write(System.Environment.NewLine);
                }
            }

            static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
            {
                string newMessage = message.Replace(oldValue, newValue);
                writer.Write(newMessage);
            }
        }

        private DateTimeOffset GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        }

        /// <summary>
        /// Gets the short string representation for a given <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <returns>A four-character string representing the log level (e.g., "info", "warn").</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="logLevel"/> is not a recognized value.</exception>
        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), $"Unknown log level: {logLevel}")
            };
        }

        /// <summary>
        /// Gets the console colors (foreground and background) for a given <see cref="LogLevel"/>.
        /// Color behavior can be influenced by <see cref="FormatterOptions"/> and detected operating system.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <returns>A <see cref="ConsoleColors"/> struct containing the foreground and background colors.</returns>
        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            // We shouldn't be outputting color codes for Android/Apple mobile platforms,
            // they have no shell (adb shell is not meant for running apps) and all the output gets redirected to some log file.
            bool disableColors = (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled) ||
                (FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && (IsAndroidOrAppleMobile));
            if (disableColors)
            {
                return new ConsoleColors(null, null);
            }
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            return logLevel switch
            {
                LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
                LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
                LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
                LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
                _ => new ConsoleColors(null, null) // Should not happen due to GetLogLevelString check, but as a fallback.
            };
        }

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider, bool singleLine)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                bool paddingNeeded = !singleLine;
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (paddingNeeded)
                    {
                        paddingNeeded = false;
                        state.Write(_messagePadding);
                        state.Write("=> ");
                    }
                    else
                    {
                        state.Write(" => ");
                    }
                    state.Write(scope);
                }, textWriter);

                if (!paddingNeeded && !singleLine)
                {
                    textWriter.Write(System.Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// A private struct to store foreground and background console color pairs.
        /// </summary>
        private readonly struct ConsoleColors
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConsoleColors"/> struct.
            /// </summary>
            /// <param name="foreground">The foreground color. Null for default.</param>
            /// <param name="background">The background color. Null for default.</param>
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            /// <summary>
            /// Gets the foreground console color.
            /// </summary>
            public ConsoleColor? Foreground { get; }

            /// <summary>
            /// Gets the background console color.
            /// </summary>
            public ConsoleColor? Background { get; }
        }
    }
}
