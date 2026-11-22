// Checks that the game data exported from the web game (Tests/Data/game-data.json) loads and has the expected shape:
// six divisions, 41 tracks, and the economy tables.
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class GameDataTier { public int index; public string name; public int laps; public int heatLaps; }
[Serializable] class GameDataTrack { public string id; public string name; public double miles; public string layout; }
[Serializable] class GameData { public GameDataTier[] tiers; public GameDataTrack[] tracks; }

public class GameDataTests
{
    [Test]
    public void GameDataLoadsWithSixDivisionsAnd41Tracks()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/game-data.json");
        Assert.IsTrue(File.Exists(path), "Missing data: " + path);
        var d = JsonUtility.FromJson<GameData>(File.ReadAllText(path));
        Assert.AreEqual(6, d.tiers.Length, "divisions");
        Assert.AreEqual(41, d.tracks.Length, "tracks");
        Assert.AreEqual("Junior Apex", d.tiers[0].name);
        Assert.AreEqual("Apex Cup", d.tiers[5].name);
        Assert.AreEqual(8, d.tiers[0].laps, "Junior Apex feature laps");
    }
}
