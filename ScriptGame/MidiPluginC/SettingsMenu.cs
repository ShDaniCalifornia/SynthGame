using Godot;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using System.Collections.Generic;

namespace Synthesizer.ScriptGame.MidiPluginC
{
    /// <summary>
    /// SettingsMenu — класс, отвечающий за меню настроек приложения
    /// Загружает списки доступных устройств (MIDI, аудио) и SoundFont'ов,
    /// а также применяет выбранные пользователем настройки
    /// </summary>
    public partial class SettingsMenu : Node
    {
        // ==================== СПИСКИ УСТРОЙСТВ ====================

        private List<string> midiDevices = new List<string>();
        private List<string> audioDevices = new List<string>();
        private List<string> soundFonts = new();

        // ==================== СИГНАЛЫ ====================

        /// <summary>
        /// Сигнал, который срабатывает при выборе нового SoundFont
        /// Передаёт путь к выбранному файлу
        /// </summary>
        [Signal]
        public delegate void SoundFontChangedEventHandler(string fontPath);


        // ==================== ССЫЛКИ ====================

        /// <summary>
        /// Ссылка на AudioManager для управления звуком
        /// Устанавливается из MainScene
        /// </summary>
        public AudioManager audioManager;


        // ==================== ИНИЦИАЛИЗАЦИЯ ====================

        /// <summary>
        /// Вызывается автоматически при загрузке сцены
        /// Загружаем все списки устройств и файлов
        /// </summary>
        public override void _Ready()
        {
            PopulateMidiDevices();
            PopulateAudioDevices();
            LoadSoundFonts();
        }

        // ==================== ЗАПОЛНЕНИЕ СПИСКОВ ====================

        /// <summary>
        /// Заполняет список доступных MIDI-устройств
        /// </summary>
        public void PopulateMidiDevices()
        {
            midiDevices.Clear();
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                midiDevices.Add(MidiIn.DeviceInfo(i).ProductName);
            }
        }

        /// <summary>
        /// Заполняет список доступных аудио-устройств (наушники, колонки и т.д.)
        /// С защитой от COM-ошибок.
        /// </summary>
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

        /// <summary>
        /// Сканирует папку SoundFonts и собирает все файлы с расширением .sf2
        /// </summary>
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

        // ==================== МЕТОДЫ ДЛЯ GODOT (OptionButton) ====================

        /// <summary>
        /// Возвращает список MIDI-устройств в формате Godot Array
        /// </summary>
        public Godot.Collections.Array GetMidiDevices()
        {
            var arr = new Godot.Collections.Array();
            foreach (var device in midiDevices)
                arr.Add(device);
            return arr;
        }

        /// <summary>
        /// Возвращает список аудио-устройств в формате Godot Array
        /// </summary>
        public Godot.Collections.Array GetAudioDevices()
        {
            var arr = new Godot.Collections.Array();
            foreach (var device in audioDevices)
                arr.Add(device);
            return arr;
        }

        /// <summary>
        /// Возвращает список доступных SoundFont'ов
        /// </summary>
        public Godot.Collections.Array GetSoundFonts()
        {
            var arr = new Godot.Collections.Array();
            foreach (var sf in soundFonts)
                arr.Add(sf);
            return arr;
        }


        // ==================== ПРИМЕНЕНИЕ НАСТРОЕК ====================

        /// <summary>
        /// Применяет выбранные настройки MIDI и аудио устройства
        /// Сохраняет их в конфигурационный файл
        /// </summary>
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

        /// <summary>
        /// Вызывается при выборе SoundFont в OptionButton (из GDScript)
        /// Сохраняет выбор и уведомляет другие части приложения
        /// </summary>
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