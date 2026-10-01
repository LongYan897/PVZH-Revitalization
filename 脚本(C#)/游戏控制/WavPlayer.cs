using Godot;

namespace Controller;

public static class WavPlayer
{
    public static void Play(string path, float volumeDb = 0f, float pitchScale = 1f)
    {
        var stream = GD.Load<AudioStream>(path);
        if (stream == null)
        {
            return;
        }

        var root = Main.AudioContainer;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb,
            PitchScale = pitchScale
        };

        root.AddChild(player);

        player.Finished += player.QueueFree;

        player.Play();
    }
}