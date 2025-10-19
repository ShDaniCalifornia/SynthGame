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

    [Signal]
    public delegate void LessonLoadedEventHandler(Godot.Collections.Dictionary<int, Godot.Collections.Dictionary<string, Variant>> data);

    public class ModuleCompleteResult
    {
        public bool Success { get; set; }
        public int NewExp { get; set; }
        public int NewLevel { get; set; }
    }

    public Godot.Collections.Dictionary CompleteModule(int userId, int moduleId)
    {
        var progress = _db.Progress
            .FirstOrDefault(p => p.IDUsers == userId && p.IDModule == moduleId);

        var user = _db.Users.FirstOrDefault(u => u.IDUsers == userId);
        if (user == null)
        {
            return new Godot.Collections.Dictionary
            {
                { "success", false },
                { "new_exp", 0 },
                { "new_level", 0 }
            };
        }

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

        if (isFirstCompletion)
        {
            expGained = 50;
            user.Exp += expGained;

            int newLevel = user.Level;
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



    public async void LoadLesson(int moduleId, int userId)
    {
        var theoryStepsRaw = await _db.Theory
            .Include(t => t.TheoryType)
            .Include(t => t.Answer)
            .Where(t => t.IDModule == moduleId)
            .OrderBy(t => t.IDTheory)
            .ToListAsync();

        var stepsDict = new Godot.Collections.Dictionary<int, Godot.Collections.Dictionary<string, Variant>>();
        int index = 1;

        foreach (var step in theoryStepsRaw)
        {
            var stepData = new Godot.Collections.Dictionary<string, Variant>();
            stepData.Add("text", step.Text);
            stepData.Add("type", step.TheoryType.TypeText);

            var keysArray = new Godot.Collections.Array();
            bool isGamemode = false;

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
