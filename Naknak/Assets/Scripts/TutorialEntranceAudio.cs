using System.Collections;
using UnityEngine;

/// <summary>Approaching footsteps followed by the shop's door bell, underneath the S0/S1 fade.</summary>
public static class TutorialEntranceAudio
{
    private static AudioClip bell;
    public static bool IsPlaying { get; private set; }
    public static int PlayCount { get; private set; }

    public static IEnumerator Play(GameObject owner, AudioClip footsteps)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.pitch = .8f;
        source.volume = .65f;
        source.clip = footsteps;
        IsPlaying = true;
        PlayCount++;
        try
        {
            source.Play();
            yield return new WaitForSecondsRealtime(1.15f);
            for (float elapsed = 0; elapsed < .1f; elapsed += Time.unscaledDeltaTime)
            { source.volume = .65f * (1 - elapsed / .1f); yield return null; }
            source.Stop();
            source.pitch = 1;
            source.volume = .6f;
            if (bell == null) bell = CreateBell();
            source.PlayOneShot(bell);
            yield return new WaitForSecondsRealtime(bell.length);
        }
        finally
        {
            IsPlaying = false;
            if (source != null) Object.Destroy(source);
        }
    }

    private static AudioClip CreateBell()
    {
        const int sampleRate = 44100;
        var samples = new float[sampleRate];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)sampleRate;
            float attack = Mathf.Min(1, t / .004f);
            float tail = Mathf.Clamp01((1 - t) / .08f);
            samples[i] = attack * tail * (.5f * Mathf.Exp(-5 * t) * Mathf.Sin(2 * Mathf.PI * 1760 * t) +
                .2f * Mathf.Exp(-7 * t) * Mathf.Sin(2 * Mathf.PI * 2794 * t) +
                .12f * Mathf.Exp(-10 * t) * Mathf.Sin(2 * Mathf.PI * 4210 * t));
        }
        var clip = AudioClip.Create("Shop entrance bell", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
