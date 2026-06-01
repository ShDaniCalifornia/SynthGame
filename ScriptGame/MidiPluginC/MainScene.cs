using Godot;
using Synthesizer.ScriptGame.MidiPluginC; // C# сервисы (MidiControllers, AudioManager)
using System.IO;



/// <summary>
/// MainScene — главная сцена приложения
/// Отвечает за инициализацию всех основных систем при запуске
/// </summary>
public partial class MainScene : Node
{
    private MidiControllers midiControllers; // Класс для работы с MIDI-клавиатурой
    private AudioManager audioManager; // Класс для управления звуком


    /// <summary>
    /// Метод, который вызывается автоматически при готовности сцены (_Ready)
    /// Здесь происходит вся начальная инициализация
    /// </summary>
    public override void _Ready()
    {
        // Создаём и добавляем в дерево сцен MIDI-контроллер
        midiControllers = new MidiControllers();
        AddChild(midiControllers);

        // Создаём и добавляем менеджер аудио
        audioManager = new AudioManager();
        AddChild(audioManager);

        // Получаем ссылку на менеджер клавиш (визуализация клавиатуры)
        var keyPadManager = GetNode<Node>("/root/MainScene/Keys");
        midiControllers.keyPadManager = keyPadManager;

        // Передаём AudioManager в настройки, чтобы можно было менять устройство из меню
        var settingsScript = GetNode<SettingsMenu>("TutorialUI/SettingsContext/SettingsScript");
        if (settingsScript != null)
        {
            settingsScript.audioManager = audioManager;
        }

        // ==================== ЗАГРУЗКА НАСТРОЕК ====================

        var config = new ConfigFile();
        int midiIndex = 0;
        int audioIndex = 0;
        string selectedSoundFont = "FluidR3_GM.sf2";

        // Пытаемся загрузить сохранённые настройки
        if (config.Load("user://settings.cfg") == Error.Ok)
        {
            midiIndex = (int)config.GetValue("midi", "device", 0);
            audioIndex = (int)config.GetValue("audio", "device", 0);
            if (config.HasSectionKey("soundfont", "selected"))
                selectedSoundFont = (string)config.GetValue("soundfont", "selected");
        }


        // ==================== ЗАГРУЗКА SOUNDFONT ====================

        string soundFontPath = "C:\\Users\\Chibi\\Desktop\\SynthGame\\SynthGame\\synthesizer\\SoundFonts\\FluidR3_GM.sf2";

        if (!File.Exists(soundFontPath))
        {
            GD.PrintErr($"SoundFont не найден: {soundFontPath}");
            return;
        }
        // Загружаем SoundFont и инициализируем звук
        audioManager.LoadSoundFont(soundFontPath);
        audioManager.InitializeAudio();
        audioManager.SetAudioDevice(audioIndex);

        // ==================== ПОДКЛЮЧЕНИЕ MIDI ====================

        // Подключаемся к выбранному MIDI-устройству
        midiControllers.ConnectMidi(midiIndex);
        // Подписываемся на события нажатия и отпускания клавиш
        midiControllers.Connect(nameof(MidiControllers.NoteOn), new Callable(this, nameof(OnNoteOn)));
        midiControllers.Connect(nameof(MidiControllers.NoteOff), new Callable(this, nameof(OnNoteOff)));
    }

    /// <summary>
    /// Вызывается при нажатии ноты на MIDI-клавиатуре
    /// Передаёт ноту и силу нажатия в AudioManager
    /// </summary>
    private void OnNoteOn(int note, int velocity)
    {
        audioManager.PlayNote(note, velocity);
    }

    /// <summary>
    /// Вызывается при отпускании ноты
    /// </summary>
    private void OnNoteOff(int note)
    {
        audioManager.StopNote(note);
    }
}
