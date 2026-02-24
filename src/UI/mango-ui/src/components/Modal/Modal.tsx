import { type ReactNode, type MouseEvent } from 'react';
import './Modal.css';

interface ModalProps {
    title: string;
    isOpen: boolean;
    onClose: () => void;
    children: ReactNode;
    footer?: ReactNode;
    size?: 'sm' | 'md' | 'lg';
}

export const Modal = ({
    title,
    isOpen,
    onClose,
    children,
    footer,
    size = 'md'
}: ModalProps) => {
    if (!isOpen) return null;

    const handleOverlayClick = () => {
        onClose();
    };

    const handleModalClick = (e: MouseEvent) => {
        e.stopPropagation();
    };

    return (
        <div className="modal-overlay" onClick={handleOverlayClick}>
            <div className={`modal modal--${size}`} onClick={handleModalClick}>
                <div className="modal__header">
                    <h2>{title}</h2>
                    <button className="modal__close" onClick={onClose} aria-label="Close">✕</button>
                </div>

                <div className="modal__body">
                    {children}
                </div>

                {footer && (
                    <div className="modal__footer">
                        {footer}
                    </div>
                )}
            </div>
        </div>
    );
};
