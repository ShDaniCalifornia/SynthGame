using MeltySynth; // Библиотека для синтеза звука на основе SoundFont (.sf2)
using NAudio.Wave; // Интерфейс IWaveProvider для интеграции с NAudio
using System;


/// <summary>
/// MidiSampleProvider — основной класс, отвечающий за синтез звука в реальном времени
/// Реализует интерфейс IWaveProvider, чтобы NAudio мог использовать его как источник аудио
/// </summary>
public class MidiSampleProvider : IWaveProvider
{

    // ==================== СТАТИЧЕСКИЕ И КОНСТАНТНЫЕ ПОЛЯ ====================

    /// <summary>
    /// Формат аудио: 44.1 кГц, 2 канала (стерео), 32-bit float (стандартный высококачественный формат)
    /// </summary>
    private static readonly WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    // ==================== ОСНОВНЫЕ КОМПОНЕНТЫ ====================

    /// <summary>
    /// Синтезатор MeltySynth — отвечает за генерацию звука по MIDI-сообщениям
    /// </summary>
    private readonly MeltySynth.Synthesizer synthesizer;

    /// <summary>
    /// Секвенсор — используется для проигрывания целых MIDI-файлов
    /// </summary>
    private readonly MidiFileSequencer sequencer;

    /// <summary>
    /// Объект для синхронизации (thread-safety)
    /// Защищает синтезатор от одновременного доступа из разных потоков
    /// </summary>
    private readonly object mutex = new();


    // ==================== КОНСТРУКТОР ====================

    /// <summary>
    /// Создаёт провайдер звука и загружает SoundFont
    /// </summary>
    public MidiSampleProvider(string soundFontPath)
    {
        // Инициализируем синтезатор с частотой дискретизации 44100
        synthesizer = new MeltySynth.Synthesizer(soundFontPath, format.SampleRate);
        sequencer = new MidiFileSequencer(synthesizer);
        // Увеличиваем общую громкость
        synthesizer.MasterVolume = 2.0f;

        // Устанавливаем инструмент по умолчанию (Program 0) на канал 0
        synthesizer.ProcessMidiMessage(0, 0xC0, 0, 0);
    }

    // ==================== СВОЙСТВА ====================

    /// <summary>
    /// Возвращает формат аудио. Требуется интерфейсом IWaveProvider
    /// </summary>
    public WaveFormat WaveFormat => format;


    // ==================== МЕТОДЫ УПРАВЛЕНИЯ ====================

    /// <summary>
    /// Проигрывает MIDI-файл
    /// </summary>
    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }
    }

    /// <summary>
    /// Останавливает проигрывание MIDI-файла
    /// </summary>
    public void Stop()
    {
        lock (mutex)
        {
            sequencer.Stop();
        }
    }

    /// <summary>
    /// Проигрывает одну ноту (используется при нажатии клавиш)
    /// </summary>
    public void PlayNote(int note, int channel, int velocity)
    {
        lock (mutex)
        {
            synthesizer.NoteOn(channel, note, velocity);
        }
    }

    /// <summary>
    /// Останавливает ноту (при отпускании клавиши)
    /// </summary>
    public void StopNote(int note, int channel)
    {
        lock (mutex)
        {
            synthesizer.NoteOff(channel, note);
        }
    }

    /// <summary>
    /// Смена текущего инструмента (Program Change)
    /// </summary>
    public void SetInstrument(int program)
    {
        lock (mutex)
        {
            synthesizer.ProcessMidiMessage(0, 0xC0, program, 0);
        }
    }

    // ==================== ГЛАВНЫЙ МЕТОД РЕНДЕРА ЗВУКА ====================

    /// <summary>
    /// Основной метод, который NAudio вызывает для получения следующего куска аудио
    /// Здесь происходит реальная генерация звука синтезатором
    /// </summary>
    public int Read(byte[] buffer, int offset, int count)
    {
        int floatsCount = count / 4; // Переводим байты в float
        var floatBuffer = new float[floatsCount];

        // Рендерим звук в float-массив
        synthesizer.RenderInterleaved(floatBuffer);

        // Копируем float-семплы в byte-массив (требуется NAudio)
        int pos = offset;
        foreach (var sample in floatBuffer)
        {
            var bytes = BitConverter.GetBytes(sample);
            Array.Copy(bytes, 0, buffer, pos, 4);
            pos += 4;
        }
        return count; // Возвращаем, сколько байт было "прочитано"
    }

    /// <summary>
    /// Альтернативный метод рендера
    /// </summary>
    private int Render(float[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            synthesizer.RenderInterleaved(buffer.AsSpan(offset, count));
            return count;
        }
    }
}
