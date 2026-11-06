using WebApp.Models;

namespace WebApp.Tests;

// Grading of fill-in-the-blank code questions (text comparison, the code is never run)
public class CodeAnswerCheckerTests
{
    private static QuestQuestion SumQuestion() => new()
    {
        Type = "code",
        StartCode = "public static int sum(int a, int b) {\n    return ____;\n}",
        AcceptedCode = "a + b\nb + a"
    };

    [Fact]
    [Trait("Category", "Positive")]
    public void Accepts_answer_with_different_spacing_return_and_semicolon()
    {
        var question = SumQuestion();

        Assert.True(CodeAnswerChecker.IsCorrect(question, "a+b"));
        Assert.True(CodeAnswerChecker.IsCorrect(question, "  return   b +  a ; "));
    }

    [Fact]
    [Trait("Category", "Negative")]
    public void Rejects_wrong_answer_and_wrong_capitalisation()
    {
        var question = SumQuestion();

        Assert.False(CodeAnswerChecker.IsCorrect(question, "a - b"));
        Assert.False(CodeAnswerChecker.IsCorrect(question, "A + B"));   // Java is case-sensitive
        Assert.False(CodeAnswerChecker.IsCorrect(question, "returna+b")); // "return" must be its own word
    }

    [Fact]
    [Trait("Category", "Negative")]
    public void Rejects_empty_answers_and_questions_without_accepted_code()
    {
        Assert.False(CodeAnswerChecker.IsCorrect(SumQuestion(), ""));
        Assert.False(CodeAnswerChecker.IsCorrect(SumQuestion(), "   ;  "));
        Assert.False(CodeAnswerChecker.IsCorrect(new QuestQuestion { Type = "code", AcceptedCode = null }, "a + b"));
    }
}
