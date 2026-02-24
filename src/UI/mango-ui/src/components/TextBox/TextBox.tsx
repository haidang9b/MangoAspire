import { forwardRef, type InputHTMLAttributes } from 'react';
import '../CommonUI.css';

interface TextBoxProps extends InputHTMLAttributes<HTMLInputElement> {
    label?: string;
    error?: string;
    containerClassName?: string;
}

export const TextBox = forwardRef<HTMLInputElement, TextBoxProps>(
    ({ label, error, containerClassName = '', className = '', ...props }, ref) => {
        const inputId = props.id || (label ? `input-${label.replace(/\s+/g, '-').toLowerCase()}` : undefined);

        return (
            <div className={`form-group ${containerClassName}`}>
                {label && <label htmlFor={inputId}>{label}</label>}
                <input
                    ref={ref}
                    id={inputId}
                    className={`form-control ${error ? 'form-control--error' : ''} ${className}`}
                    {...props}
                />
                {error && <span className="error-msg">{error}</span>}
            </div>
        );
    }
);

TextBox.displayName = 'TextBox';
