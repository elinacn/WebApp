using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Tests;

// How the quest form is turned into database rows (shared by the form and the seeder)
public class QuestFormMapperTests
{
    private static Quest Map(CreateQuestQuestionViewModel question)
    {
        var quest = new Quest();
        QuestFormMapper.ApplyToEntity(quest, new CreateQuestViewModel
        {
            Title = "  Test quest ",
            Difficulty = "Apprentice",
            Questions = [question]
        });
        return quest;
    }

    [Fact]
    [Trait("Category", "Positive")]
    public void Multiple_choice_skips_empty_options_and_keeps_correct_answers_after_renumbering()
    {
        var quest = Map(new CreateQuestQuestionViewModel
        {
            Type = "multipleChoice",
            Question = "Pick the even numbers",
            Options =
            [
                new() { Id = "a", Text = "2" },
                new() { Id = "b", Text = "  " },   // empty: dropped, so c and d move up to b and c
                new() { Id = "c", Text = "3" },
                new() { Id = "d", Text = "4" }
            ],
            CorrectOptionIds = ["a", "d"]
        });

        Assert.Equal("Test quest", quest.Title);
        var question = Assert.Single(quest.Questions);
        Assert.Equal(["a", "b", "c"], question.Options.Select(o => o.OptionKey));
        Assert.Equal(["2", "3", "4"], question.Options.Select(o => o.Text));
        Assert.Equal("a,c", question.CorrectOptionId);   // "4" was d, is now c
    }

    [Fact]
    [Trait("Category", "Positive")]
    public void Free_text_answers_are_trimmed_deduplicated_and_stored_one_per_line()
    {
        var quest = Map(new CreateQuestQuestionViewModel
        {
            Type = "freeText",
            Question = "Which method returns a String's length?\r\nAnswer without the dot.",
            CaseSensitive = false,
            AcceptedAnswers = [" length() ", "", "LENGTH()", "size()"]
        });

        var question = Assert.Single(quest.Questions);
        Assert.Equal("length()\nsize()", question.AcceptedAnswers);   // "LENGTH()" is a duplicate when not case-sensitive
        Assert.Equal("Which method returns a String's length?\nAnswer without the dot.", question.Question);
        Assert.Empty(question.Options);
    }
}
