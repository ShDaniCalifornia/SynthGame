using Godot;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.IO;

public partial class AudioManager : Control
{
    private WasapiOut outputDevice;
    private MidiSampleProvider sampleProvider;

    private int currentAudioDeviceIndex = 0;


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

    public void SetWaveProvider(MidiSampleProvider provider)
    {
        sampleProvider = provider;
        RestartAudioOutput();
    }

    public void SetAudioDevice(int deviceIndex)
    {
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        if (deviceIndex >= 0 && deviceIndex < devices.Count)
        {
            currentAudioDeviceIndex = deviceIndex;  // сохраняем текущий индекс устройства
            SelectOutputDevice(deviceIndex);        // сохраняем в конфиг
            RestartAudioOutput();
        }
    }

    public void SelectOutputDevice(int index)
    {
        currentAudioDeviceIndex = index;

        var config = new ConfigFile();
        config.Load("user://settings.cfg");
        config.SetValue("audio", "device", index);
        config.Save("user://settings.cfg");

        RestartAudioOutput();
    }

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

        outputDevice = new WasapiOut(devices[index], AudioClientShareMode.Shared, false, 100);
        outputDevice.Init(sampleProvider);
        outputDevice.Play();
    }

    private void StopAudio()
    {
        outputDevice?.Stop();
        outputDevice?.Dispose();
        outputDevice = null;
    }

    public int GetSavedAudioDeviceIndex()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") == Error.Ok)
            return (int)config.GetValue("audio", "device", 0);
        return 0;
    }


    public void LoadSoundFont(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        var newProvider = new MidiSampleProvider(path);
        SetWaveProvider(newProvider);
    }

    public void SetInstrument(int program)
    {
        sampleProvider?.SetInstrument(program);
    }

    public void PlayNote(int note, int velocity)
    {
        sampleProvider?.PlayNote(note, 0, velocity);
    }

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
