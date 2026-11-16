// A labelled form control with an optional hint and validation error underneath.
// The control itself is passed as children, e.g. <FormField label="Title" error={errors.title}><input … /></FormField>
export default function FormField({ label, htmlFor, error, hint, children }) {
  return (
    <div className="field">
      {label && <label htmlFor={htmlFor}>{label}</label>}
      {children}
      {hint && !error && <span className="hint">{hint}</span>}
      {error && (
        <p className="field-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
