using Godot;
using NAudio.CoreAudioApi; // Для работы с аудио-устройствами   
using NAudio.Wave; // Основная библиотека NAudio для воспроизведения звука
using System.IO; // Для проверки существования файлов SoundFont


/// <summary>
/// AudioManager — главный класс, отвечающий за звук в приложении
/// Управляет выводом аудио, выбором устройства (наушники), загрузкой SoundFont и проигрыванием нот
/// </summary>
public partial class AudioManager : Control
{
    private WasapiOut outputDevice; // Устройство вывода звука (NAudio)
    private MidiSampleProvider sampleProvider; // Провайдер, который генерирует звук из MIDI

    private int currentAudioDeviceIndex = 0; // Текущий индекс выбранного аудио-устройства


    /// <summary>
    /// Инициализация аудио при запуске сцены
    /// Пытается восстановить сохранённое устройство и отдаёт приоритет наушникам
    /// </summary>
    public void InitializeAudio()
    {
        int savedIndex = GetSavedAudioDeviceIndex();

        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        if (savedIndex >= devices.Count || savedIndex < 0)
            savedIndex = 0;

        var savedDeviceName = devices[savedIndex].FriendlyName.ToLower();

        if (savedDeviceName.Contains("headphones") || savedDeviceName.Contains("наушники"))
        {
            currentAudioDeviceIndex = savedIndex;
        }
        else
        {
            currentAudioDeviceIndex = FindHeadphonesIndex();
        }

        RestartAudioOutput();
    }

    public override void _ExitTree()
    {
        StopAudio();
    }

    public void PlayMenuMusic(string path)
    {
        // Реализация воспроизведения музыки
    }


    /// <summary>
    /// Устанавливает новый провайдер звука (MidiSampleProvider) и перезапускает вывод.
    /// </summary>
    public void SetWaveProvider(MidiSampleProvider provider)
    {
        sampleProvider = provider;
        RestartAudioOutput();
    }


    /// <summary>
    /// Публичный метод для смены аудио-устройства
    /// </summary>
    public void SetAudioDevice(int deviceIndex)
    {
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        if (deviceIndex >= 0 && deviceIndex < devices.Count)
        {
            currentAudioDeviceIndex = deviceIndex;  // сохраняет текущий индекс устройства
            SelectOutputDevice(deviceIndex);        // сохраняет в конфиг
            RestartAudioOutput();
        }
    }


    /// <summary>
    /// Сохраняет выбранное аудио-устройство в файл настроек в Godot
    /// </summary>
    public void SelectOutputDevice(int index)
    {
        currentAudioDeviceIndex = index;

        var config = new ConfigFile();
        config.Load("user://settings.cfg");
        config.SetValue("audio", "device", index);
        config.Save("user://settings.cfg");

        RestartAudioOutput();
    }


    /// <summary>
    /// Перезапускает аудио-поток: останавливает старый и запускает новый с выбранным устройством
    /// </summary>
    private void RestartAudioOutput()
    {
        StopAudio();
        if (sampleProvider == null) return;

        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        if (devices.Count == 0) return;

        int index = currentAudioDeviceIndex;
        if (index < 0 || index >= devices.Count)
            index = 0;

        // Создаёт WasapiOut с низкой задержкой
        outputDevice = new WasapiOut(devices[index], AudioClientShareMode.Shared, false, 100);
        outputDevice.Init(sampleProvider); // Подключает наш MIDI-провайдер
        outputDevice.Play(); // Запускает воспроизведение
    }


    /// <summary>
    /// Корректно останавливает и освобождает аудио-устройство.
    /// </summary>
    private void StopAudio()
    {
        outputDevice?.Stop();
        outputDevice?.Dispose();
        outputDevice = null;
    }


    /// <summary>
    /// Загружает последний выбранный индекс устройства из файла настроек
    /// </summary>
    public int GetSavedAudioDeviceIndex()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") == Error.Ok)
            return (int)config.GetValue("audio", "device", 0);
        return 0; // По умолчанию — первое устройство
    }


    /// <summary>
    /// Загружает новый SoundFont (.sf2) и применяет его
    /// </summary>
    public void LoadSoundFont(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        var newProvider = new MidiSampleProvider(path);
        SetWaveProvider(newProvider);
    }


    /// <summary>
    /// Смена текущего инструмента (звучания)
    /// </summary>
    public void SetInstrument(int program)
    {
        sampleProvider?.SetInstrument(program);
    }


    /// <summary>
    /// Проигрывает ноту (вызывается из Godot при нажатии клавиши)
    /// </summary>
    public void PlayNote(int note, int velocity)
    {
        sampleProvider?.PlayNote(note, 0, velocity);
    }


    /// <summary>
    /// Останавливает ноту (отпускание клавиши)
    /// </summary>
    public void StopNote(int note)
    {
        sampleProvider?.StopNote(note, 0);
    }

    private int FindHeadphonesIndex()
    {
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        for (int i = 0; i < devices.Count; i++)
        {
            var name = devices[i].FriendlyName.ToLower();
            if (name.Contains("headphones") || name.Contains("наушники"))
            {
                return i;
            }
        }

        return 0;
    }
}
