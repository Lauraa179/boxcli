using System;
using System.Linq;
using static BoxCLI.CommandUtilities.ColorConstants;

namespace BoxCLI.CommandUtilities
{
    internal static class Reporter
    {
        public static bool NoColor { get; set; }
        public static bool PrefixOutput { get; set; }

        public static string Colorize(string value, Func<string, string> colorizeFunc)
            => NoColor ? value : colorizeFunc(value);

        public static void WriteSuccess(string message)
            => WriteLine(Prefix("success:   ", Colorize(message, x => Bold + Green + x + Reset)));

        public static void WriteError(string message)
            => WriteLine(Prefix("error:   ", Colorize(message, x => Bold + Red + x + Reset)), true);

        public static void WriteWarning(string message)
            => WriteLine(Prefix("warn:    ", Colorize(message, x => Bold + Yellow + x + Reset)));

        public static void WriteWarningNoNewLine(string message)
            => Write(Prefix("warn:    ", Colorize(message, x => Bold + Yellow + x + Reset)));

        public static void WriteInformation(string message)
            => WriteLine(Prefix("info:    ", message));

        public static void WriteData(string message)
            => WriteLine(Prefix("data:    ", Colorize(message, x => Bold + Gray + x + Reset)));

        private static string Prefix(string prefix, string value)
            => PrefixOutput
                ? string.Join(
                    Environment.NewLine,
                    value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => prefix + l))
                : value;

        private static void WriteLine(string value, bool isErr = false)
        {
            if (NoColor && isErr)
            {
                Console.WriteLine(value);
            }
            else if(NoColor && !isErr)
            {
                Console.Error.WriteLine(value);
            }
            else
            {
                AnsiConsole.WriteLine(value, isErr);
            }
        }
        private static void Write(string value, bool isErr = false)
        {
            if (NoColor && isErr)
            {
                Console.Write(value);
            }
            else if(NoColor && !isErr)
            {
                Console.Error.Write(value);
            }
            else
            {
                AnsiConsole.Write(value);
            }
        }
    }

}