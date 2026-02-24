import { useState } from 'react';
import { useApi } from '../../hooks/useApi';
import type { Product, CatalogType } from '../../types/product';
import type { CreateProductRequest, UpdateProductRequest } from '../../api/productsApi';

// ─── helpers ─────────────────────────────────────────────────────────────────

export interface FormState {
    name: string;
    price: string;
    description: string;
    categoryName: string;
    catalogTypeId: string;
    imageUrl: string;
    stock: string;
}

export const EMPTY_FORM: FormState = {
    name: '',
    price: '',
    description: '',
    categoryName: '',
    catalogTypeId: '',
    imageUrl: '',
    stock: '0',
};

function stripHtml(html: string) {
    const div = document.createElement('div');
    div.innerHTML = html;
    return div.textContent || '';
}

export function productToForm(p: Product): FormState {
    return {
        name: p.name,
        price: String(p.price),
        description: stripHtml(p.description),
        categoryName: p.catalogType?.type ?? '',
        catalogTypeId: p.catalogTypeId ? String(p.catalogTypeId) : '',
        imageUrl: p.imageUrl ?? '',
        stock: String(p.stock),
    };
}

// ─── Component ────────────────────────────────────────────────────────────────

export interface ProductFormModalProps {
    editing: Product | null;
    catalogTypes: CatalogType[];
    onClose: () => void;
    onSaved: () => void;
}

import { TextBox, SelectBox, Modal } from '../../components';

export function ProductFormModal({ editing, catalogTypes, onClose, onSaved }: ProductFormModalProps) {
    const { products: productsService } = useApi();
    const [form, setForm] = useState<FormState>(editing ? productToForm(editing) : EMPTY_FORM);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const set = (field: keyof FormState) => (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
    ) => setForm(prev => ({ ...prev, [field]: e.target.value }));

    const handleCatalogTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const val = e.target.value;
        const type = catalogTypes.find(ct => String(ct.id) === val);
        setForm(prev => ({
            ...prev,
            catalogTypeId: val,
            categoryName: type?.type ?? prev.categoryName,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setError(null);

        const base: CreateProductRequest = {
            name: form.name.trim(),
            price: parseFloat(form.price),
            description: form.description.trim(),
            categoryName: form.categoryName.trim(),
            catalogTypeId: form.catalogTypeId ? parseInt(form.catalogTypeId) : undefined,
            imageUrl: form.imageUrl.trim(),
            stock: parseInt(form.stock),
        };

        try {
            let result;
            if (editing) {
                const payload: UpdateProductRequest = { ...base, id: editing.id };
                result = await productsService.updateProduct(payload);
            } else {
                result = await productsService.createProduct(base);
            }

            if (result.isError) {
                setError(result.errorMessage ?? 'An error occurred.');
            } else {
                onSaved();
            }
        } catch {
            setError('Could not connect to the server.');
        } finally {
            setSaving(false);
        }
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
                    <TextBox
                        id="pm-name"
                        label="Name *"
                        value={form.name}
                        onChange={set('name')}
                        required
                    />
                    <TextBox
                        id="pm-price"
                        label="Price *"
                        type="number"
                        step="0.01"
                        min="0.01"
                        value={form.price}
                        onChange={set('price')}
                        required
                    />
                </div>

                <div className="form-row">
                    <SelectBox
                        id="pm-type"
                        label="Category"
                        value={form.catalogTypeId}
                        onChange={handleCatalogTypeChange}
                    >
                        <option value="">— select —</option>
                        {catalogTypes.map(ct => (
                            <option key={ct.id} value={ct.id}>{ct.type}</option>
                        ))}
                    </SelectBox>

                    <TextBox
                        id="pm-stock"
                        label="Stock *"
                        type="number"
                        min="0"
                        value={form.stock}
                        onChange={set('stock')}
                        required
                    />
                </div>

                <TextBox
                    id="pm-imageUrl"
                    label="Image URL *"
                    value={form.imageUrl}
                    onChange={set('imageUrl')}
                    required
                />

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

                {error && <p className="form-error">⚠️ {error}</p>}
            </form>
        </Modal>
    );
}
