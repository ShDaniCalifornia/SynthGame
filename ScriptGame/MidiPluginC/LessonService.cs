using Godot;
using Microsoft.EntityFrameworkCore;
using Synthesizer.Database.Contexts;
using Synthesizer.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LessonService : Node
{
    private SynthesizerContext _db = new SynthesizerContext();

    // ==================== СИГНАЛ ====================
    /// <summary>
    /// Сигнал, который отправляется после загрузки урока
    /// Передаёт структурированные данные урока в GDScript
    /// </summary>
    [Signal]
    public delegate void LessonLoadedEventHandler(Godot.Collections.Dictionary<int, Godot.Collections.Dictionary<string, Variant>> data);

    // ==================== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ====================

    /// <summary>
    /// Вспомогательный класс для возврата результата завершения модуля
    /// </summary>
    public class ModuleCompleteResult
    {
        public bool Success { get; set; }
        public int NewExp { get; set; }
        public int NewLevel { get; set; }
    }


    // ==================== ОСНОВНЫЕ МЕТОДЫ ====================
    /// <summary>
    /// Завершает модуль для пользователя -  начисляет опыт, повышает уровень при необходимости
    /// Возвращает результат в формате Godot Dictionary
    /// </summary>
    public Godot.Collections.Dictionary CompleteModule(int userId, int moduleId)
    {
        // Ищет прогресс пользователя по этому модулю
        var progress = _db.Progress
            .FirstOrDefault(p => p.IDUsers == userId && p.IDModule == moduleId);

        // Получает данные пользователя
        var user = _db.Users.FirstOrDefault(u => u.IDUsers == userId);
        if (user == null)
        {
            // Пользователь не найден
            return new Godot.Collections.Dictionary
            {
                { "success", false },
                { "new_exp", 0 },
                { "new_level", 0 }
            };
        }

        // Если модуль уже был пройден ранее - ничего не начисляет
        if (progress != null && progress.Completed)
        {
            return new Godot.Collections.Dictionary
            {
                { "success", true  },
                { "new_exp", 0 },
                { "new_level", user.Level },
                { "total_exp", user.Exp }
            };
        }

        bool isFirstCompletion = false;

        if (progress == null)
        {
            // Первый раз проходим модуль — создаёт запись
            progress = new Progress
            {
                IDUsers = userId,
                IDModule = moduleId,
                Completed = true,
            };
            _db.Progress.Add(progress);
            isFirstCompletion = true;
        }
        else if (!progress.Completed)
        {
            progress.Completed = true;
            isFirstCompletion = true;
        }

        int expGained = 0;

        // Начисляет опыт только при первом завершении
        if (isFirstCompletion)
        {
            expGained = 50;
            user.Exp += expGained;

            int newLevel = user.Level; // каждый следующий уровень требует на 100 опыта больше
            while (user.Exp >= newLevel * 100)
            {
                newLevel++;
            }
            user.Level = newLevel;

            _db.Users.Update(user);
            _db.SaveChanges();
        }

        return new Godot.Collections.Dictionary
        {
            { "success", true },
            { "new_exp", expGained },
            { "new_level", user.Level },
            { "total_exp", user.Exp }
        };
    }


    /// <summary>
    /// Асинхронно загружает все шаги урока (теорию) по ID модуля
    /// Формирует сложную структуру Godot Dictionary и отправляет через сигнал
    /// </summary>
    public async void LoadLesson(int moduleId, int userId)
    {
        // Загружаем все шаги теории для модуля с включением связанных таблиц
        var theoryStepsRaw = await _db.Theory
            .Include(t => t.TheoryType)
            .Include(t => t.Answer)
            .Where(t => t.IDModule == moduleId)
            .OrderBy(t => t.IDTheory)
            .ToListAsync();

        // Словарь для хранения шагов урока
        var stepsDict = new Godot.Collections.Dictionary<int, Godot.Collections.Dictionary<string, Variant>>();
        int index = 1;

        foreach (var step in theoryStepsRaw)
        {
            var stepData = new Godot.Collections.Dictionary<string, Variant>();
            stepData.Add("text", step.Text); // Текст шага
            stepData.Add("type", step.TheoryType.TypeText); // Тип - "теория" или "задание"

            var keysArray = new Godot.Collections.Array();
            bool isGamemode = false;

            // Если это задание — парсим клавиши/ноты из ответов
            if (step.TheoryType.TypeText.Equals("задание", StringComparison.OrdinalIgnoreCase))
            {
                var allAnswers = step.Answer
                    .SelectMany(a => a.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim())
                    .Distinct();

                foreach (var answer in allAnswers)
                {
                    keysArray.Add(answer);
                }

                // Проверка - есть ли Gamemode (режим задания)
                isGamemode = step.Answer.Any(a => a.IsGamemode);
            }

            stepData.Add("keys", keysArray);
            stepData.Add("is_gamemode", isGamemode);
            stepsDict.Add(index, stepData);
            index++;
        }

        var lessonData = new Godot.Collections.Dictionary<string, Variant>();
        lessonData.Add("steps", stepsDict);
        lessonData.Add("module_id", moduleId);

        EmitSignal(nameof(LessonLoaded), lessonData);
    }
}
