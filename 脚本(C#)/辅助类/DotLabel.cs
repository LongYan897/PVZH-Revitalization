using Godot;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Pack;

[GlobalClass, Tool]
public partial class DotLabel : Label
{
    private static readonly Regex IntSegmentRegex =
        new Regex(@"(?<!\d)-?\d+", RegexOptions.Compiled);

    public event Action<string> TextChanged;

    [Export]
    public int Amount
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Text))
                return 0;
            try { return ((DotInt)Text).To<int>(); }
            catch (FormatException) { return 0; }
        }
        set => SetText(value);
    }

    public void SetText(int amount)
    {
        var dot = new DotInt(amount);
        Text = dot;
        TextChanged?.Invoke(Text);
        NotifyPropertyListChanged();
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == "text")
        {
            string newText = value.AsString();
            string formatted = Normalize(newText);

            if (Text != formatted)
            {
                bool result = base._Set(property, formatted);
                TextChanged?.Invoke(formatted);
                NotifyPropertyListChanged();
                return result;
            }
            return true;
        }

        return base._Set(property, value);
    }

    public override void _Process(double delta)
    {
        string formatted = Normalize(Text);
        if (Text != formatted)
        {
            Text = formatted;
            TextChanged?.Invoke(formatted);
            NotifyPropertyListChanged();
        }
    }

    private static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new DotInt(0).ToString();

        return IntSegmentRegex.Replace(raw, match =>
        {
            if (int.TryParse(match.Value,
                             NumberStyles.Integer,
                             CultureInfo.InvariantCulture,
                             out int value))
            {
                return new DotInt(value).ToString();
            }
            return match.Value;
        });
    }
}