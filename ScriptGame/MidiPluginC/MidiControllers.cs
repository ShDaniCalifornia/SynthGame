using Godot;
using NAudio.Midi;
using System;


/// <summary>
/// MidiControllers — отвечает за подключение и обработку физической MIDI-клавиатуры
/// Получает события нажатия/отпускания клавиш и передаёт их дальше в AudioManager и визуализацию
/// </summary>
public partial class MidiControllers : Node
{
    // ==================== СИГНАЛЫ ====================

    /// <summary>
    /// Сигнал, который срабатывает при нажатии ноты
    /// </summary>
    [Signal]
    public delegate void NoteOnEventHandler(int note, int velocity);

    /// <summary>
    /// Сигнал, который срабатывает при отпускании ноты
    /// </summary>
    [Signal]
    public delegate void NoteOffEventHandler(int note);


    // ==================== ПОЛЯ ====================

    private MidiIn midiIn;  // Объект NAudio для приёма MIDI-сообщений
    public Node keyPadManager; // Ссылка на менеджер визуализации клавиатуры (Keys)


    // ==================== ОСНОВНЫЕ МЕТОДЫ ====================

    /// <summary>
    /// Подключается к MIDI-устройству по индексу
    /// </summary>
    public void ConnectMidi(int deviceIndex)
    {
        if (MidiIn.NumberOfDevices == 0) return;
        if (deviceIndex < 0 || deviceIndex >= MidiIn.NumberOfDevices) return;

        try
        {
            midiIn = new MidiIn(deviceIndex);
            // Подписываемся на события
            midiIn.MessageReceived += MidiIn_MessageReceived;
            midiIn.ErrorReceived += (_, e) => GD.PrintErr("MIDI ошибка: " + e.ToString());
            midiIn.Start(); // Запускаем прослушивание MIDI-сообщений
        }
        catch (Exception e)
        {
        }
    }

    /// <summary>
    /// Основной обработчик всех входящих MIDI-сообщений
    /// </summary>
    private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        // Обработка нажатия клавиши
        if (e.MidiEvent is NoteOnEvent noteOn)
        {
            int note = noteOn.NoteNumber;
            int velocity = noteOn.Velocity;

            if (velocity > 0) // velocity = 0 иногда используется как NoteOff
            {
                // Используем CallDeferred, чтобы вызвать в главном потоке Godo
                CallDeferred(nameof(EmitNoteOn), note, velocity);
                keyPadManager?.CallDeferred("press_midi_key", note); // Подсвечиваем клавишу на экране
            }
            // Обработка отпускания клавиши
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

    /// <summary>
    /// Вспомогательный метод для безопасного вызова сигнала NoteOn
    /// </summary>
    private void EmitNoteOn(int note, int velocity)
    {
        EmitSignal(nameof(NoteOn), note, velocity);
    }

    /// <summary>
    /// Вспомогательный метод для безопасного вызова сигнала NoteOff
    /// </summary>
    private void EmitNoteOff(int note)
    {
        EmitSignal(nameof(NoteOff), note);
    }

    /// <summary>
    /// Корректно отключаем MIDI при закрытии сцены
    /// </summary>
    public override void _ExitTree()
    {
        midiIn?.Stop();
        midiIn?.Dispose();
    }
}
