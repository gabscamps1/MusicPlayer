using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    [Header("Song Data Packs (Albums)")]
    public List<SongData> songDataPacks;

    [Header("Generated Playlist")]
    public List<Track> playlist;

    [Header("UI References")]
    public TMP_Text songNameText;
    public TMP_Text authorText;
    public TMP_Text timeCurrentText;
    public TMP_Text timeTotalText;
    public Image thumbnailImage;
    public Slider progressBar;

    [Header("Icons")]
    public Image playPauseIcon;
    public Sprite iconPlay;
    public Sprite iconPause;

    public Image loopIcon;
    public Sprite iconLoopOn;
    public Sprite iconLoopOff;

    [Header("Playlist UI")]
    public Transform playlistContentParent;
    public GameObject playlistButtonPrefab;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Control Buttons")]
    public Button playPauseButton;
    public Button nextButton;
    public Button previousButton;
    public Button forward10Button;
    public Button backward10Button;
    public Toggle loopToggle;

    private List<SongButtonUI> playlistButtons = new List<SongButtonUI>();

    private int currentIndex = 0;
    private bool isPaused = true;

    // thumbnail spin
    public float thumbnailSpinSpeed = 40f;

    private bool isDragging = false;

    private void Start()
    {
        GeneratePlaylist();
        GeneratePlaylistButtons();

        // guard clause: if playlist empty, skip
        if (playlist.Count == 0)
        {
            Debug.LogWarning("MusicPlayer: playlist empty.");
            return;
        }

        LoadSong(currentIndex);
        UpdateSelectedSongUI();

        // control bindings
        playPauseButton.onClick.RemoveAllListeners();
        playPauseButton.onClick.AddListener(TogglePlayPause);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextSong);

        previousButton.onClick.RemoveAllListeners();
        previousButton.onClick.AddListener(PreviousSong);

        forward10Button.onClick.RemoveAllListeners();
        forward10Button.onClick.AddListener(() => Seek(10f));

        backward10Button.onClick.RemoveAllListeners();
        backward10Button.onClick.AddListener(() => Seek(-10f));

        progressBar.onValueChanged.AddListener(OnSeekBarChanged);

        loopToggle.onValueChanged.AddListener(delegate { UpdateLoopIcon(); PlayLoopAnimation(); });
        UpdateLoopIcon();
    }

    private void Update()
    {
        if (audioSource.clip == null) return;

        // if not dragging update UI from audio
        if (!isDragging)
            UpdateProgressUI();

        // thumbnail rotation while playing
        if (audioSource.isPlaying)
            thumbnailImage.transform.Rotate(0f, 0f, thumbnailSpinSpeed * Time.deltaTime);

        // auto next when song ends (and not in single-loop)
        if (!isPaused && !loopToggle.isOn && audioSource.time >= audioSource.clip.length - 0.05f)
        {
            NextSong();
        }

        // single-track loop handled by toggle (optional earlier behavior)
        if (!isPaused && loopToggle.isOn && audioSource.time >= audioSource.clip.length - 0.05f)
        {
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    // ---------------- Playlist ------------------
    private void GeneratePlaylist()
    {
        playlist = new List<Track>();

        foreach (var pack in songDataPacks)
        {
            if (pack == null) continue;
            foreach (var track in pack.tracks)
            {
                playlist.Add(track);
            }
        }
    }

    private void GeneratePlaylistButtons()
    {
        // clear existing
        foreach (Transform child in playlistContentParent)
            Destroy(child.gameObject);

        playlistButtons.Clear();

        for (int i = 0; i < playlist.Count; i++)
        {
            var track = playlist[i];
            GameObject btnObj = Instantiate(playlistButtonPrefab, playlistContentParent);

            SongButtonUI ui = btnObj.GetComponent<SongButtonUI>();
            ui.Setup(track, i, this);
            ui.Deselect(); // ensure start state

            playlistButtons.Add(ui);
        }
    }

    public void UpdateSelectedSongUI()
    {
        // highlight current + update playing icons
        for (int i = 0; i < playlistButtons.Count; i++)
        {
            bool isCurrent = (i == currentIndex);
            if (isCurrent)
            {
                playlistButtons[i].Select();
                playlistButtons[i].SetPlayingState(audioSource.isPlaying);
            }
            else
            {
                playlistButtons[i].Deselect();
                playlistButtons[i].SetIdle();
            }
        }
    }

    // ---------------- Player Core ------------------
    private void LoadSong(int index)
    {
        if (playlist.Count == 0) return;

        currentIndex = index;
        Track track = playlist[currentIndex];

        audioSource.clip = track.clip;
        songNameText.text = track.trackName;
        authorText.text = track.author;
        thumbnailImage.sprite = track.thumbnail;

        timeTotalText.text = FormatTime(track.clip.length);

        // reset thumbnail rotation
        thumbnailImage.transform.rotation = Quaternion.identity;

        Play();
        UpdateSelectedSongUI();
    }

    public void LoadSongFromExternal(int index)
    {
        LoadSong(index);
    }

    // ---------------- Play / Pause ------------------
    public void TogglePlayPause()
    {
        if (audioSource.isPlaying)
            Pause();
        else
            Play();

        UpdatePlayPauseIcon();
        PlayPlayButtonAnimation();
    }

    public void Play()
    {
        if (audioSource.clip == null) return;

        audioSource.Play();
        isPaused = false;
        UpdatePlayPauseIcon();
        PlayPlayButtonAnimation();
        // update playlist icons
        UpdateSelectedSongUI();
    }

    public void Pause()
    {
        audioSource.Pause();
        isPaused = true;
        UpdatePlayPauseIcon();
        PlayPlayButtonAnimation();
        // update playlist icons
        UpdateSelectedSongUI();
    }

    private void UpdatePlayPauseIcon()
    {
        if (playPauseIcon != null)
            playPauseIcon.sprite = audioSource.isPlaying ? iconPause : iconPlay;
    }

    private void UpdateLoopIcon()
    {
        if (loopIcon != null)
            loopIcon.sprite = loopToggle.isOn ? iconLoopOn : iconLoopOff;
    }

    // ---------------- Animations ------------------
    private void PlayPlayButtonAnimation()
    {
        if (playPauseButton == null) return;

        LeanTween.cancel(playPauseButton.gameObject);
        playPauseButton.transform.localScale = Vector3.one;
        LeanTween.scale(playPauseButton.gameObject, Vector3.one * 1.15f, 0.12f).setEaseOutQuad()
            .setOnComplete(() => LeanTween.scale(playPauseButton.gameObject, Vector3.one, 0.12f).setEaseOutQuad());
    }

    private void PlayLoopAnimation()
    {
        if (loopIcon == null) return;

        LeanTween.cancel(loopIcon.gameObject);
        LeanTween.rotateZ(loopIcon.gameObject, 20f, 0.09f).setEaseOutQuad()
            .setOnComplete(() => LeanTween.rotateZ(loopIcon.gameObject, 0f, 0.09f).setEaseOutQuad());
    }

    // ---------------- Navigation ------------------
    public void NextSong()
    {
        if (playlist.Count == 0) return;

        currentIndex++;
        if (currentIndex >= playlist.Count)
            currentIndex = 0;

        LoadSong(currentIndex);
    }

    public void PreviousSong()
    {
        if (playlist.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = playlist.Count - 1;

        LoadSong(currentIndex);
    }

    // ---------------- Seek ------------------
    public void Seek(float seconds)
    {
        if (audioSource.clip == null) return;

        audioSource.time = Mathf.Clamp(audioSource.time + seconds, 0f, audioSource.clip.length);
    }

    public void OnSeekBarChanged(float value)
    {
        if (audioSource.clip == null) return;
        if (isDragging)
            audioSource.time = value * audioSource.clip.length;
    }

    public void StartDrag() => isDragging = true;

    public void EndDrag()
    {
        isDragging = false;
        if (audioSource.clip != null)
            audioSource.time = progressBar.value * audioSource.clip.length;
    }

    // ---------------- UI Update ------------------
    private void UpdateProgressUI()
    {
        if (audioSource.clip == null) return;

        timeCurrentText.text = FormatTime(audioSource.time);
        progressBar.value = audioSource.time / audioSource.clip.length;

        // keep icons in sync
        UpdateSelectedSongUI();
    }

    private string FormatTime(float t)
    {
        int min = (int)(t / 60);
        int sec = (int)(t % 60);
        return $"{min:00}:{sec:00}";
    }
}
