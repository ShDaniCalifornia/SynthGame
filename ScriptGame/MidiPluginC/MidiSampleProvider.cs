using MeltySynth;
using NAudio.Wave;
using System;



public class MidiSampleProvider : IWaveProvider
{
    private static readonly WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private readonly MeltySynth.Synthesizer synthesizer;
    private readonly MidiFileSequencer sequencer;
    private readonly object mutex = new();

    public MidiSampleProvider(string soundFontPath)
    {
        synthesizer = new MeltySynth.Synthesizer(soundFontPath, format.SampleRate);
        sequencer = new MidiFileSequencer(synthesizer);
        synthesizer.MasterVolume = 2.0f;

        // Задать инструмент (program) на канале 0
        synthesizer.ProcessMidiMessage(0, 0xC0, 0, 0);
    }

    public WaveFormat WaveFormat => format;

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }
    }

    public void Stop()
    {
        lock (mutex)
        {
            sequencer.Stop();
        }
    }

    public void PlayNote(int note, int channel, int velocity)
    {
        lock (mutex)
        {
            synthesizer.NoteOn(channel, note, velocity);
        }
    }

    public void StopNote(int note, int channel)
    {
        lock (mutex)
        {
            synthesizer.NoteOff(channel, note);
        }
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int floatsCount = count / 4;
        var floatBuffer = new float[floatsCount];
        synthesizer.RenderInterleaved(floatBuffer);

        int pos = offset;
        foreach (var sample in floatBuffer)
        {
            var bytes = BitConverter.GetBytes(sample);
            Array.Copy(bytes, 0, buffer, pos, 4);
            pos += 4;
        }
        return count;
    }

    private int Render(float[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            synthesizer.RenderInterleaved(buffer.AsSpan(offset, count));
            return count;
        }
    }

    public void SetInstrument(int program)
    {
        lock (mutex)
        {
            synthesizer.ProcessMidiMessage(0, 0xC0, program, 0);
        }
    }
}
