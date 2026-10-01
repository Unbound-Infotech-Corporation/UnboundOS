using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>QWERTY grid for controller-first text. No IME, no third-party hook.</summary>
public static class OnScreenKeyboardLayout
{
    public static IReadOnlyList<OnScreenKey> Qwerty { get; } = Build();

    public static string Apply(string current, OnScreenKey key)
    {
        current ??= "";
        if (!key.IsAction)
        {
            return current + key.Value;
        }

        return key.Id switch
        {
            "space" => current + " ",
            "back" => current.Length == 0 ? current : current[..^1],
            "clear" => "",
            _ => current
        };
    }

    private static IReadOnlyList<OnScreenKey> Build()
    {
        var keys = new List<OnScreenKey>();
        AddRow(keys, 0, "1234567890");
        AddRow(keys, 1, "qwertyuiop");
        AddRow(keys, 2, "asdfghjkl");
        AddRow(keys, 3, "zxcvbnm");
        keys.Add(new("space", "SPACE", " ", 4, 0, true));
        keys.Add(new("back", "DEL", "", 4, 1, true));
        keys.Add(new("clear", "CLR", "", 4, 2, true));
        return keys;
    }

    private static void AddRow(List<OnScreenKey> keys, int row, string letters)
    {
        for (var i = 0; i < letters.Length; i++)
        {
            var ch = letters[i].ToString();
            keys.Add(new(ch, ch.ToUpperInvariant(), ch, row, i, false));
        }
    }
}
