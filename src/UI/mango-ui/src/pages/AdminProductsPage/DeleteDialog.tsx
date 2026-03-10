import { useMutation } from '@tanstack/react-query';
import { useApi } from '@/hooks/useApi';
import { Modal } from '@/components';
import type { Product } from '@/types/product';

export interface DeleteDialogProps {
    product: Product;
    onCancel: () => void;
    onDeleted: () => void;
}

export function DeleteDialog({ product, onCancel, onDeleted }: DeleteDialogProps) {
    const { products: productsService } = useApi();

    const { mutate: doDelete, isPending: deleting, error } = useMutation({
        mutationFn: async () => {
            const result = await productsService.deleteProduct(product.id);
            if (result.isError) throw new Error(result.errorMessage ?? 'Failed to delete.');
        },
        onSuccess: onDeleted,
    });

    return (
        <Modal
            title="Delete Product"
            isOpen={true}
            onClose={onCancel}
            size="sm"
            footer={
                <>
                    <button className="btn-secondary" onClick={onCancel}>Cancel</button>
                    <button className="btn-danger" onClick={() => doDelete()} disabled={deleting}>
                        {deleting ? 'Deleting…' : 'Delete'}
                    </button>
                </>
            }
        >
            <p className="delete-confirm__msg">
                Are you sure you want to delete <strong>{product.name}</strong>? This cannot be undone.
            </p>
            {error && <p className="form-error">⚠️ {error.message}</p>}
        </Modal>
    );
}
