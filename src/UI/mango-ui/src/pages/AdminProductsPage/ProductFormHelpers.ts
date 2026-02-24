import type { Product } from '../../types/product';

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
