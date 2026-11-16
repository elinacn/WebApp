import { CODE_BLANK, QUESTION_TYPES } from '../constants'
import FormField from './FormField'

export const MAX_OPTIONS = 6

// Editor for one question in the quest form. `errors` are the form's errors, keyed like
// "questions[2].options"; `onChange` receives the updated question.
export default function QuestionEditor({ question, index, errors, total, onChange, onRemove, onMove }) {
  const key = (field) => `questions[${index}].${field}`
  const set = (field, value) => onChange({ ...question, [field]: value })

  const setOption = (i, changes) =>
    set(
      'options',
      question.options.map((o, n) => (n === i ? { ...o, ...changes } : o)),
    )

  // A single correct option unless the author ticks more than one
  const toggleCorrect = (i) => setOption(i, { isCorrect: !question.options[i].isCorrect })

  const setAnswer = (i, value) =>
    set(
      'acceptedAnswers',
      question.acceptedAnswers.map((a, n) => (n === i ? value : a)),
    )

  return (
    <section className="card question-editor">
      <div className="card-header">
        <h3>Question {index + 1}</h3>
        <div className="button-row">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => onMove(-1)}
            disabled={index === 0}
            aria-label="Move up"
          >
            ↑
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => onMove(1)}
            disabled={index === total - 1}
            aria-label="Move down"
          >
            ↓
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={onRemove}
            disabled={total === 1}
            aria-label={`Remove question ${index + 1}`}
          >
            🗑 Remove
          </button>
        </div>
      </div>

      <div className="form">
        <div className="form-row">
          <FormField label="Type" htmlFor={`q${index}-type`} error={errors[key('type')]}>
            <select
              id={`q${index}-type`}
              className="select"
              value={question.type}
              onChange={(e) => set('type', e.target.value)}
            >
              {QUESTION_TYPES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </FormField>
          <FormField label="Points" htmlFor={`q${index}-points`} error={errors[key('points')]}>
            <input
              id={`q${index}-points`}
              className={`input ${errors[key('points')] ? 'invalid' : ''}`}
              type="number"
              min="0"
              max="1000"
              value={question.points}
              onChange={(e) => set('points', e.target.value === '' ? '' : Number(e.target.value))}
            />
          </FormField>
        </div>

        <FormField label="Question" htmlFor={`q${index}-text`} error={errors[key('text')]}>
          <textarea
            id={`q${index}-text`}
            className={`textarea ${errors[key('text')] ? 'invalid' : ''}`}
            value={question.text}
            maxLength={1000}
            onChange={(e) => set('text', e.target.value)}
          />
        </FormField>

        {question.type === 'multipleChoice' && (
          <FormField label="Options (tick the correct ones)" error={errors[key('options')]}>
            <div className="option-editor-list">
              {question.options.map((option, i) => (
                <div key={i} className={`option-editor ${option.isCorrect ? 'correct' : ''}`}>
                  <label className="checkbox" title="Correct answer">
                    <input type="checkbox" checked={option.isCorrect} onChange={() => toggleCorrect(i)} />
                    <span className="option-key">{String.fromCharCode(65 + i)}</span>
                  </label>
                  <input
                    className="input"
                    value={option.text}
                    maxLength={300}
                    placeholder={`Option ${String.fromCharCode(65 + i)}`}
                    aria-label={`Option ${String.fromCharCode(65 + i)}`}
                    onChange={(e) => setOption(i, { text: e.target.value })}
                  />
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => set('options', question.options.filter((_, n) => n !== i))}
                    disabled={question.options.length <= 2}
                    aria-label={`Remove option ${String.fromCharCode(65 + i)}`}
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
            {question.options.length < MAX_OPTIONS && (
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => set('options', [...question.options, { text: '', isCorrect: false }])}
              >
                + Add option
              </button>
            )}
          </FormField>
        )}

        {question.type === 'code' && (
          <FormField
            label="Start code"
            htmlFor={`q${index}-code`}
            error={errors[key('startCode')]}
            hint={`Write the code with exactly one ${CODE_BLANK} where the student fills in the answer.`}
          >
            <textarea
              id={`q${index}-code`}
              className={`textarea code ${errors[key('startCode')] ? 'invalid' : ''}`}
              value={question.startCode}
              maxLength={2000}
              spellCheck={false}
              placeholder={`public static int sum(int a, int b) {\n    return ${CODE_BLANK};\n}`}
              onChange={(e) => set('startCode', e.target.value)}
            />
          </FormField>
        )}

        {question.type !== 'multipleChoice' && (
          <FormField
            label={question.type === 'code' ? 'Accepted code for the blank' : 'Accepted answers'}
            error={errors[key('acceptedAnswers')]}
            hint={
              question.type === 'code'
                ? 'Spaces, a trailing ; and a leading "return" are ignored when checking.'
                : 'Any of these counts as correct.'
            }
          >
            <div className="option-editor-list">
              {question.acceptedAnswers.map((answer, i) => (
                <div key={i} className="option-editor">
                  <input
                    className={`input ${question.type === 'code' ? 'code-input' : ''}`}
                    value={answer}
                    placeholder={question.type === 'code' ? 'a + b' : 'Answer'}
                    aria-label={`Accepted answer ${i + 1}`}
                    onChange={(e) => setAnswer(i, e.target.value)}
                  />
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => set('acceptedAnswers', question.acceptedAnswers.filter((_, n) => n !== i))}
                    disabled={question.acceptedAnswers.length <= 1}
                    aria-label={`Remove accepted answer ${i + 1}`}
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
            <div className="button-row">
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => set('acceptedAnswers', [...question.acceptedAnswers, ''])}
              >
                + Add answer
              </button>
              {question.type === 'freeText' && (
                <label className="checkbox small">
                  <input
                    type="checkbox"
                    checked={question.caseSensitive}
                    onChange={(e) => set('caseSensitive', e.target.checked)}
                  />
                  Case-sensitive
                </label>
              )}
            </div>
          </FormField>
        )}

        <FormField
          label="Explanation (optional)"
          htmlFor={`q${index}-explanation`}
          hint="Shown after the quest is submitted."
          error={errors[key('explanation')]}
        >
          <textarea
            id={`q${index}-explanation`}
            className="textarea"
            value={question.explanation}
            maxLength={1000}
            onChange={(e) => set('explanation', e.target.value)}
          />
        </FormField>
      </div>
    </section>
  )
}
