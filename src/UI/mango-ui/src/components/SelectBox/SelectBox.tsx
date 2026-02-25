import { forwardRef, type SelectHTMLAttributes } from 'react';
import '../CommonUI.css';

interface SelectBoxProps extends SelectHTMLAttributes<HTMLSelectElement> {
    label?: string;
    error?: string;
    containerClassName?: string;
    options?: { value: string | number; label: string }[];
}

export const SelectBox = forwardRef<HTMLSelectElement, SelectBoxProps>(
    ({ label, error, containerClassName = '', className = '', options, children, ...props }, ref) => {
        const selectId = props.id || (label ? `select-${label.replace(/\s+/g, '-').toLowerCase()}` : undefined);

        return (
            <div className={`form-group ${containerClassName}`}>
                {label && <label htmlFor={selectId}>{label}</label>}
                <select
                    ref={ref}
                    id={selectId}
                    className={`form-control ${error ? 'form-control--error' : ''} ${className}`}
                    {...props}
                >
                    {options ? (
                        <>
                            {options.map((opt) => (
                                <option key={opt.value} value={opt.value}>
                                    {opt.label}
                                </option>
                            ))}
                        </>
                    ) : (
                        children
                    )}
                </select>
                {error && <span className="error-msg">{error}</span>}
            </div>
        );
    }
);

SelectBox.displayName = 'SelectBox';
