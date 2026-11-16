// Renders one question and its answer input, based on the question type.
// In review mode (`result` given) the inputs are locked and right/wrong answers are marked.
export default function QuestionView({ question, answer, onChange, result }) {
  const reviewing = Boolean(result)
  const selected = answer?.selected ?? []

  const toggleOption = (key) => {
    const next = question.allowMultiple
      ? selected.includes(key)
        ? selected.filter((k) => k !== key)
        : [...selected, key]
      : [key]
    onChange({ ...answer, selected: next })
  }

  return (
    <div className="question">
      <p className="question-text">{question.text}</p>

      {question.type === 'multipleChoice' && (
        <>
          <p className="muted small">{question.allowMultiple ? 'Select all that apply.' : 'Select one answer.'}</p>
          <div className="options">
            {question.options.map((option) => {
              const isSelected = selected.includes(option.key)
              const isCorrect = reviewing && result.correctKeys.includes(option.key)
              const isWrongPick = reviewing && isSelected && !isCorrect
              return (
                <label
                  key={option.key}
                  className={`option ${isSelected ? 'selected' : ''} ${isCorrect ? 'correct' : ''} ${isWrongPick ? 'wrong' : ''}`}
                >
                  <input
                    type={question.allowMultiple ? 'checkbox' : 'radio'}
                    name={`question-${question.id}`}
                    checked={isSelected}
                    onChange={() => toggleOption(option.key)}
                    disabled={reviewing}
                  />
                  <span className="option-key">{option.key.toUpperCase()}</span>
                  <span>{option.text}</span>
                </label>
              )
            })}
          </div>
        </>
      )}

      {question.type === 'code' && <pre className="code-block">{question.startCode}</pre>}

      {(question.type === 'freeText' || question.type === 'code') && (
        <input
          className={`input ${question.type === 'code' ? 'code-input' : ''}`}
          value={answer?.text ?? ''}
          onChange={(event) => onChange({ ...answer, text: event.target.value })}
          placeholder={question.type === 'code' ? 'Code for the ____ blank' : 'Your answer'}
          aria-label="Your answer"
          autoComplete="off"
          spellCheck={question.type !== 'code'}
          disabled={reviewing}
        />
      )}

      {reviewing && !result.isCorrect && result.acceptedAnswers.length > 0 && (
        <p className="accepted">
          Accepted answer{result.acceptedAnswers.length > 1 ? 's' : ''}:{' '}
          {result.acceptedAnswers.map((a, i) => (
            <span key={a}>
              {i > 0 && ' or '}
              <code>{a}</code>
            </span>
          ))}
        </p>
      )}

      {reviewing && result.explanation && <p className="explanation">💡 {result.explanation}</p>}
    </div>
  )
}
