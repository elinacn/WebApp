using WebApp.Api.Dtos;
using WebApp.Api.Models;

namespace WebApp.Api.Services;

// Domain rules a quest must follow before it can be saved, on top of the DTO's data annotations.
// Returns errors keyed like ASP.NET's model state ("Questions[0].Options"), so the
// React form can show each message next to the right field.
public static class QuestValidator
{
    public const int MaxOptions = 6;
    public const int MaxQuestions = 50;

    public static Dictionary<string, string> Validate(QuestFormDto form)
    {
        var errors = new Dictionary<string, string>();

        if (DifficultyRules.Normalize(form.Difficulty) is null)
            errors["Difficulty"] = "Choose a difficulty.";

        if (form.Questions.Count == 0)
            errors["Questions"] = "Add at least one question.";
        else if (form.Questions.Count > MaxQuestions)
            errors["Questions"] = $"A quest can have at most {MaxQuestions} questions.";

        for (var i = 0; i < form.Questions.Count; i++)
        {
            var question = form.Questions[i];
            var prefix = $"Questions[{i}]";

            switch (question.Type)
            {
                case QuestionTypes.MultipleChoice:
                    ValidateChoice(question, prefix, errors);
                    break;
                case QuestionTypes.FreeText:
                    ValidateAnswers(question, prefix, errors);
                    break;
                case QuestionTypes.Code:
                    var blanks = (question.StartCode ?? string.Empty).Split(QuestGradingService.CodeBlank).Length - 1;
                    if (blanks != 1)
                        errors[$"{prefix}.StartCode"] = $"The start code must contain exactly one {QuestGradingService.CodeBlank} blank.";
                    ValidateAnswers(question, prefix, errors);
                    break;
                default:
                    errors[$"{prefix}.Type"] = "Choose a valid question type.";
                    break;
            }
        }

        return errors;
    }

    private static void ValidateChoice(QuestionFormDto question, string prefix, Dictionary<string, string> errors)
    {
        var filled = question.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();

        if (question.Options.Count > MaxOptions)
            errors[$"{prefix}.Options"] = $"A question can have at most {MaxOptions} options.";
        else if (filled.Count < 2)
            errors[$"{prefix}.Options"] = "Fill in at least two options.";
        else if (!filled.Any(o => o.IsCorrect))
            errors[$"{prefix}.Options"] = "Mark at least one option as correct.";
        else if (question.Options.Any(o => o.IsCorrect && string.IsNullOrWhiteSpace(o.Text)))
            errors[$"{prefix}.Options"] = "A correct option can't be empty.";
    }

    private static void ValidateAnswers(QuestionFormDto question, string prefix, Dictionary<string, string> errors)
    {
        var answers = DtoMapper.CleanAnswers(question.AcceptedAnswers);
        if (answers.Count == 0)
            errors[$"{prefix}.AcceptedAnswers"] = "Add at least one accepted answer.";
        else if (string.Join("\n", answers).Length > 1000)
            errors[$"{prefix}.AcceptedAnswers"] = "The accepted answers can be at most 1000 characters in total.";
    }
}
