import { useState } from 'react';
import { useApi } from '@/hooks/useApi';
import type { Product } from '@/types/product';

export interface DeleteDialogProps {
    product: Product;
    onCancel: () => void;
    onDeleted: () => void;
}

import { Modal } from '@/components';
import { useTranslation } from 'react-i18next';

export function DeleteDialog({ product, onCancel, onDeleted }: DeleteDialogProps) {
    const { products: productsService } = useApi();
    const { t } = useTranslation();
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
        <Modal
            title={t('common.delete') || "Delete Product"}
            isOpen={true}
            onClose={onCancel}
            size="sm"
            footer={
                <>
                    <button className="btn-secondary" onClick={onCancel}>{t('common.cancel')}</button>
                    <button className="btn-danger" onClick={handleDelete} disabled={deleting}>
                        {deleting ? t('common.loading') : t('common.delete')}
                    </button>
                </>
            }
        >
            <p className="delete-confirm__msg">
                {t('admin.deleteConfirm') || "Are you sure you want to delete"} <strong>{product.name}</strong>?
            </p>
            {error && <p className="form-error">⚠️ {error}</p>}
        </Modal>
    );
}
