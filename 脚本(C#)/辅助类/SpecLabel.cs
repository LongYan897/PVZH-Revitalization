using Card;
using Controller;
using Godot;
using System.Threading.Tasks;

namespace Pack;

[GlobalClass]
public partial class SpecLabel : RichTextLabel
{
    [Export(PropertyHint.MultilineText)]
    public string RawText
    {
        get => _rawText;
        set
        {
            value ??= string.Empty;
            if (_rawText == value) return;
            _rawText = value;
            RequestRefresh();
        }
    }
    private string _rawText = string.Empty;

    [Export] public bool ConvertLinks { get; set; } = true;
    [Export] public float ExtraSize { get; set; }

    [Export]
    public bool EnableKeywordClick
    {
        get => _enableKeywordClick;
        set
        {
            _enableKeywordClick = value;
            ApplyKeywordClickSubscription();
        }
    }
    private bool _enableKeywordClick = true;
    private bool _keywordSubscribed;

    private CardModel _model;
    private bool _ready;
    private int _version;

    public override void _Ready()
    {
        _ready = true;
        BbcodeEnabled = true;

        ApplyKeywordClickSubscription();

        if (string.IsNullOrEmpty(_rawText) && !string.IsNullOrEmpty(Text))
        {
            _rawText = Text;
            Text = string.Empty;
        }

        if (!string.IsNullOrEmpty(_rawText))
            RequestRefresh();
    }

    public SpecLabel SetContent(string raw, CardModel card = null)
    {
        _model = card;
        _rawText = raw ?? string.Empty;
        RequestRefresh();
        return this;
    }

    public async Task SetContentAsync(string raw, CardModel card = null)
    {
        _model = card;
        _rawText = raw ?? string.Empty;
        if (!_ready || !IsInsideTree()) return;
        await RefreshAsync();
    }

    public SpecLabel SetRawText(string raw) => SetContent(raw, _model);

    public void Refresh() => RequestRefresh();

    private void ApplyKeywordClickSubscription()
    {
        if (!_ready) return;

        if (_enableKeywordClick && !_keywordSubscribed)
        {
            MouseFilter = MouseFilterEnum.Stop;
            MetaClicked += OnKeywordClicked;
            _keywordSubscribed = true;
        }
        else if (!_enableKeywordClick && _keywordSubscribed)
        {
            MetaClicked -= OnKeywordClicked;
            _keywordSubscribed = false;
        }
    }

    private void RequestRefresh()
    {
        if (!_ready || !IsInsideTree()) return;
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        int myVersion = ++_version;
        bool Cancelled()
            => myVersion != _version
            || !GodotObject.IsInstanceValid(this)
            || !IsInsideTree();

        await RichTextFormatter.AppendAsync(
            this, _rawText, ConvertLinks, ExtraSize, Cancelled, model: _model);
    }

    private void OnKeywordClicked(Variant metaVariant)
    {
        string raw = metaVariant.AsString();

        string key = raw;
        string arg = null;
        char sep = '\0';

        int bar = raw.IndexOf('|');
        if (bar >= 0)
        {
            key = raw.Substring(0, bar);
            var rest = raw.Substring(bar + 1).Split('|');
            if (rest.Length >= 2)
            {
                arg = rest[0];
                sep = rest[1].Length > 0 ? rest[1][0] : '\0';
            }
        }

        if (CardModel.HasTemplate(key))
        {
            GetViewport().SetInputAsHandled();
            var template = CardModel.GetTemplate(key);
            if (template != null)
                CardDes.ExchangeDescription(template);
            return;
        }

        if (RichTextFormatter.TryGetKeywordDescription(_model, key, arg, sep, out string desc))
        {
            GetViewport().SetInputAsHandled();
            Vector2 globalPos = GlobalPosition + GetLocalMousePosition();
            CardDesPanel.ShowAt(desc, globalPos);
        }
    }
}