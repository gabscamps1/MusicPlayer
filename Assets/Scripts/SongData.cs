using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSongData", menuName = "Music/New Song Data")]
public class SongData : ScriptableObject
{
    public string albumName;
    public List<Track> tracks = new List<Track>();
}
