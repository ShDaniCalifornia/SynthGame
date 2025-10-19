using Godot;
using NAudio.Midi;
using System;

public partial class MidiControllers : Node
{
    [Signal]
    public delegate void NoteOnEventHandler(int note, int velocity);

    [Signal]
    public delegate void NoteOffEventHandler(int note);

    private MidiIn midiIn;
    public Node keyPadManager;

    public void ConnectMidi(int deviceIndex)
    {
        if (MidiIn.NumberOfDevices == 0) return;
        if (deviceIndex < 0 || deviceIndex >= MidiIn.NumberOfDevices) return;

        try
        {
            midiIn = new MidiIn(deviceIndex);
            midiIn.MessageReceived += MidiIn_MessageReceived;
            midiIn.ErrorReceived += (_, e) => GD.PrintErr("MIDI ошибка: " + e.ToString());
            midiIn.Start();
        }
        catch (Exception e)
        {
        }
    }

    private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteOnEvent noteOn)
        {
            int note = noteOn.NoteNumber;
            int velocity = noteOn.Velocity;

            if (velocity > 0)
            {
                // Через главный поток вызываем сигнал
                CallDeferred(nameof(EmitNoteOn), note, velocity);
                keyPadManager?.CallDeferred("press_midi_key", note);
            }
            else
            {
                CallDeferred(nameof(EmitNoteOff), note);
                keyPadManager?.CallDeferred("release_midi_key", note);
            }
        }
        else if (e.MidiEvent is NoteEvent noteOff && noteOff.CommandCode == MidiCommandCode.NoteOff)
        {
            int note = noteOff.NoteNumber;
            CallDeferred(nameof(EmitNoteOff), note);
            keyPadManager?.CallDeferred("release_midi_key", note);
        }
    }

    private void EmitNoteOn(int note, int velocity)
    {
        EmitSignal(nameof(NoteOn), note, velocity);
    }

    private void EmitNoteOff(int note)
    {
        EmitSignal(nameof(NoteOff), note);
    }

    public override void _ExitTree()
    {
        midiIn?.Stop();
        midiIn?.Dispose();
    }
}
