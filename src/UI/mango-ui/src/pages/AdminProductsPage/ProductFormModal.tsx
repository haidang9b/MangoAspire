import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { useApi } from '@/hooks/useApi';
import { TextBox, SelectBox, Modal } from '@/components';
import { EMPTY_FORM, productToForm, type FormState } from './ProductFormHelpers';
import type { Product, CatalogType } from '@/types/product';
import type { CreateProductRequest, UpdateProductRequest } from '@/api/productsApi';

export interface ProductFormModalProps {
    editing: Product | null;
    catalogTypes: CatalogType[];
    onClose: () => void;
    onSaved: () => void;
}

export function ProductFormModal({ editing, catalogTypes, onClose, onSaved }: ProductFormModalProps) {
    const { products: productsService } = useApi();
    const [form, setForm] = useState<FormState>(editing ? productToForm(editing) : EMPTY_FORM);

    const set = (field: keyof FormState) =>
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
            setForm(prev => ({ ...prev, [field]: e.target.value }));

    const handleCatalogTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const val = e.target.value;
        const type = catalogTypes.find(ct => String(ct.id) === val);
        setForm(prev => ({
            ...prev,
            catalogTypeId: val,
            categoryName: type?.type ?? prev.categoryName,
        }));
    };

    const { mutate: save, isPending: saving, error: mutationError } = useMutation({
        mutationFn: async () => {
            const base: CreateProductRequest = {
                name: form.name.trim(),
                price: parseFloat(form.price),
                description: form.description.trim(),
                categoryName: form.categoryName.trim(),
                catalogTypeId: form.catalogTypeId ? parseInt(form.catalogTypeId) : undefined,
                imageUrl: form.imageUrl.trim(),
                stock: parseInt(form.stock),
            };

            const result = editing
                ? await productsService.updateProduct({ ...base, id: editing.id } as UpdateProductRequest)
                : await productsService.createProduct(base);

            if (result.isError) throw new Error(result.errorMessage ?? 'An error occurred.');
        },
        onSuccess: onSaved,
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        save();
    };

    return (
        <Modal
            title={editing ? 'Edit Product' : 'New Product'}
            isOpen={true}
            onClose={onClose}
            footer={
                <>
                    <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
                    <button type="submit" form="product-form" className="btn-primary" disabled={saving}>
                        {saving ? 'Saving…' : editing ? 'Save Changes' : 'Create Product'}
                    </button>
                </>
            }
        >
            <form id="product-form" className="modal__form" onSubmit={handleSubmit}>
                <div className="form-row">
                    <TextBox id="pm-name" label="Name *" value={form.name} onChange={set('name')} required />
                    <TextBox id="pm-price" label="Price *" type="number" step="0.01" min="0.01" value={form.price} onChange={set('price')} required />
                </div>

                <div className="form-row">
                    <SelectBox id="pm-type" label="Category" value={form.catalogTypeId} onChange={handleCatalogTypeChange}>
                        <option value="">— select —</option>
                        {catalogTypes.map(ct => (
                            <option key={ct.id} value={ct.id}>{ct.type}</option>
                        ))}
                    </SelectBox>
                    <TextBox id="pm-stock" label="Stock *" type="number" min="0" value={form.stock} onChange={set('stock')} required />
                </div>

                <TextBox id="pm-imageUrl" label="Image URL *" value={form.imageUrl} onChange={set('imageUrl')} required />

                <div className="form-group">
                    <label htmlFor="pm-description">Description *</label>
                    <textarea
                        id="pm-description"
                        className="form-control"
                        rows={4}
                        value={form.description}
                        onChange={set('description')}
                        required
                    />
                </div>

                {mutationError && <p className="form-error">⚠️ {mutationError.message}</p>}
            </form>
        </Modal>
    );
}
