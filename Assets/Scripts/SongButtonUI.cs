using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text trackNameText;
    public TMP_Text authorText;
    public Image thumbnailImage;
    public Image background;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.85f, 0.93f, 1f);

    [Header("Play/Pause Icons")]
    public Image statusIcon;
    public Sprite iconPlaying;
    public Sprite iconPaused;
    public Sprite iconIdle;

    private int index;
    private MusicPlayer player;

    // internal state
    private bool isSelected = false;

    public void Setup(Track track, int index, MusicPlayer player)
    {
        trackNameText.text = track.trackName;
        authorText.text = track.author;
        thumbnailImage.sprite = track.thumbnail;

        this.index = index;
        this.player = player;

        // initial visuals
        if (background != null) background.color = normalColor;
        transform.localScale = Vector3.one;
        SetIdle();

        // ensure button callback
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            player.LoadSongFromExternal(this.index);
            player.UpdateSelectedSongUI();
        });
    }

    // Called by MusicPlayer to set playing icon
    public void SetPlayingState(bool isPlaying)
    {
        if (statusIcon == null) return;

        statusIcon.sprite = isPlaying ? iconPlaying : iconPaused;

        // small pulse when starts playing
        if (isPlaying)
        {
            LeanTween.cancel(gameObject); // cancel any existing
            LeanTween.scale(gameObject, Vector3.one * 1.06f, 0.12f).setEaseOutQuad()
                .setOnComplete(() => LeanTween.scale(gameObject, Vector3.one * (isSelected ? 1.05f : 1f), 0.12f).setEaseOutQuad());
        }
    }

    public void SetIdle()
    {
        if (statusIcon != null) statusIcon.sprite = iconIdle;
    }

    // Called when this becomes the selected (current) track
    public void Select()
    {
        isSelected = true;
        if (background != null) LeanTween.color(background.gameObject, selectedColor, 0.12f).setEaseOutQuad();
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.one * 1.05f, 0.12f).setEaseOutQuad();
    }

    // Called when deselected
    public void Deselect()
    {
        isSelected = false;
        if (background != null) LeanTween.color(background.gameObject, normalColor, 0.12f).setEaseOutQuad();
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.one, 0.12f).setEaseOutQuad();
    }
}
