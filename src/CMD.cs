using System;

namespace Stevehead;

/// <summary>
/// Helper methods for the Console.
/// </summary>
public static class CMD
{
    /// <summary>
    /// Changes either the foreground or background colors of the console, returning an <see cref="IDisposable"/> that
    /// once disposed, will revert the colors to what they were prior to this method being called.
    /// </summary>
    /// 
    /// <param name="foreground">The optional new foreground color.</param>
    /// <param name="background">The optional new background color.</param>
    /// <returns>A <see cref="IDisposable"/> that will revert the colors once disposed.</returns>
    public static IDisposable ChangeColor(ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        ConsoleColor oldFG = Console.ForegroundColor;
        ConsoleColor oldBG = Console.BackgroundColor;
        
        SetForegroundColor(foreground);
        SetBackgroundColor(background);

        return new CmdColorChanger()
        {
            Foreground = oldFG,
            Background = oldBG,
        };
    }

    private static void SetBackgroundColor(ConsoleColor? newColor)
    {
        if (newColor.HasValue)
        {
            Console.BackgroundColor = newColor.Value;
        }
    }

    private static void SetForegroundColor(ConsoleColor? newColor)
    {
        if (newColor.HasValue)
        {
            Console.ForegroundColor = newColor.Value;
        }
    }

    private sealed class CmdColorChanger : IDisposable
    {
        public required ConsoleColor? Background { get; set; }

        public required ConsoleColor? Foreground { get; set; }

        public void Dispose()
        {
            try
            {
                SetBackgroundColor(Background);
                SetForegroundColor(Foreground);
            }
            finally
            {
                Background = null;
                Foreground = null;
            }
        }
    }
}
