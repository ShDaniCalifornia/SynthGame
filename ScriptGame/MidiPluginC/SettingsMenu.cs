using Godot;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using System.Collections.Generic;

namespace Synthesizer.ScriptGame.MidiPluginC
{
    public partial class SettingsMenu : Node
    {
        private List<string> midiDevices = new List<string>();
        private List<string> audioDevices = new List<string>();
        private List<string> soundFonts = new();
        [Signal]
        public delegate void SoundFontChangedEventHandler(string fontPath);


        public AudioManager audioManager;

        public override void _Ready()
        {
            PopulateMidiDevices();
            PopulateAudioDevices();
            LoadSoundFonts();
        }

        public void PopulateMidiDevices()
        {
            midiDevices.Clear();
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                midiDevices.Add(MidiIn.DeviceInfo(i).ProductName);
            }
        }

        public void PopulateAudioDevices()
        {
            audioDevices.Clear();
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                string name;
                try
                {
                    name = device.FriendlyName;
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    GD.PrintErr($"Не удалось получить имя устройства {device.ID}: {e.Message}");
                    name = "[Неизвестное устройство]";
                }
                audioDevices.Add(name);
            }
        }

        public void LoadSoundFonts()
        {
            soundFonts.Clear();
            var dirPath = "res://SoundFonts";

            if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dirPath)))
            {
                return;
            }

            var dir = DirAccess.Open(dirPath);
            if (dir == null)
            {
                return;
            }

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (!string.IsNullOrEmpty(fileName))
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".sf2"))
                {
                    soundFonts.Add(fileName);
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }

        public Godot.Collections.Array GetMidiDevices()
        {
            var arr = new Godot.Collections.Array();
            foreach (var device in midiDevices)
                arr.Add(device);
            return arr;
        }

        public Godot.Collections.Array GetAudioDevices()
        {
            var arr = new Godot.Collections.Array();
            foreach (var device in audioDevices)
                arr.Add(device);
            return arr;
        }

        public Godot.Collections.Array GetSoundFonts()
        {
            var arr = new Godot.Collections.Array();
            foreach (var sf in soundFonts)
                arr.Add(sf);
            return arr;
        }

        public void ApplySettings(int midiIndex, int audioIndex)
        {
            var midi = GetNode<MidiControllers>("/root/MidiControllers");
            midi?.ConnectMidi(midiIndex);

            audioManager?.SelectOutputDevice(audioIndex);

            var config = new ConfigFile();
            config.Load("user://settings.cfg");
            config.SetValue("midi", "device", midiIndex);
            config.SetValue("audio", "device", audioIndex);
            config.Save("user://settings.cfg");
        }

        // Вызывается при выборе SoundFont в OptionButton
        public void OnSoundFontSelected(int index)
        {
            if (index < 0 || index >= soundFonts.Count)
                return;

            var selectedSf = soundFonts[index];

            // Сохраняем выбор в конфиг
            var config = new ConfigFile();
            config.Load("user://settings.cfg");
            config.SetValue("soundfont", "selected", selectedSf);
            config.Save("user://settings.cfg");

            EmitSignal(nameof(SoundFontChangedEventHandler), selectedSf);
        }
    }
}