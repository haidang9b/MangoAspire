import { useState } from 'react';
import { useApi } from '../../hooks/useApi';
import type { Product } from '../../types/product';

export interface DeleteDialogProps {
    product: Product;
    onCancel: () => void;
    onDeleted: () => void;
}

export function DeleteDialog({ product, onCancel, onDeleted }: DeleteDialogProps) {
    const { products: productsService } = useApi();
    const [deleting, setDeleting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleDelete = async () => {
        setDeleting(true);
        try {
            const result = await productsService.deleteProduct(product.id);
            if (result.isError) setError(result.errorMessage ?? 'Failed to delete.');
            else onDeleted();
        } catch {
            setError('Could not connect to the server.');
        } finally {
            setDeleting(false);
        }
    };

    return (
        <div className="modal-overlay" onClick={onCancel}>
            <div className="modal modal--sm" onClick={e => e.stopPropagation()}>
                <div className="modal__header">
                    <h2>Delete Product</h2>
                    <button className="modal__close" onClick={onCancel} aria-label="Close">✕</button>
                </div>
                <p className="delete-confirm__msg">
                    Are you sure you want to delete <strong>{product.name}</strong>? This cannot be undone.
                </p>
                {error && <p className="form-error">⚠️ {error}</p>}
                <div className="modal__actions">
                    <button className="btn-secondary" onClick={onCancel}>Cancel</button>
                    <button className="btn-danger" onClick={handleDelete} disabled={deleting}>
                        {deleting ? 'Deleting…' : 'Delete'}
                    </button>
                </div>
            </div>
        </div>
    );
}
