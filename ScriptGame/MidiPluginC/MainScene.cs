using Godot;
using Synthesizer.ScriptGame.MidiPluginC;
using System.IO;

public partial class MainScene : Node
{
    private MidiControllers midiControllers;
    private AudioManager audioManager;

    public override void _Ready()
    {
        midiControllers = new MidiControllers();
        AddChild(midiControllers);

        audioManager = new AudioManager();
        AddChild(audioManager);

        var keyPadManager = GetNode<Node>("/root/MainScene/Keys");
        midiControllers.keyPadManager = keyPadManager;

        var settingsScript = GetNode<SettingsMenu>("TutorialUI/SettingsContext/SettingsScript");
        if (settingsScript != null)
        {
            settingsScript.audioManager = audioManager;
        }

        // Загружаем настройки
        var config = new ConfigFile();
        int midiIndex = 0;
        int audioIndex = 0;
        string selectedSoundFont = "FluidR3_GM.sf2";

        if (config.Load("user://settings.cfg") == Error.Ok)
        {
            midiIndex = (int)config.GetValue("midi", "device", 0);
            audioIndex = (int)config.GetValue("audio", "device", 0);
            if (config.HasSectionKey("soundfont", "selected"))
                selectedSoundFont = (string)config.GetValue("soundfont", "selected");
        }

        string soundFontPath = "C:\\Users\\Chibi\\Desktop\\SynthGame\\SynthGame\\synthesizer\\SoundFonts\\FluidR3_GM.sf2";

        if (!File.Exists(soundFontPath))
        {
            GD.PrintErr($"SoundFont не найден: {soundFontPath}");
            return;
        }
        audioManager.LoadSoundFont(soundFontPath);
        audioManager.InitializeAudio();
        audioManager.SetAudioDevice(audioIndex);

        // Подключаем MIDI
        midiControllers.ConnectMidi(midiIndex);
        midiControllers.Connect(nameof(MidiControllers.NoteOn), new Callable(this, nameof(OnNoteOn)));
        midiControllers.Connect(nameof(MidiControllers.NoteOff), new Callable(this, nameof(OnNoteOff)));
    }

    private void OnNoteOn(int note, int velocity)
    {
        audioManager.PlayNote(note, velocity);
    }

    private void OnNoteOff(int note)
    {
        audioManager.StopNote(note);
    }
}
