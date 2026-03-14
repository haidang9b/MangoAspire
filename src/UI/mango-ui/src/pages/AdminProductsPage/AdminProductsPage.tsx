import { useState } from 'react';
import { useApi } from '@/hooks/useApi';
import { useFetch } from '@/hooks/useFetch';
import { useTranslation } from 'react-i18next';
import { PageMetadata, SearchBox, SelectBox, Pagination } from '@/components';
import { ProductFormModal } from './ProductFormModal';
import { DeleteDialog } from './DeleteDialog';
import type { Product, CatalogType } from '@/types/product';
import type { PaginatedItems } from '@/types/api';
import { useProductsSearchParams } from '@/hooks';
import { CACHE_KEYS, PAGE_SIZE_OPTIONS } from '@/constants';

import './AdminProductsPage.css';

export function AdminProductsPage() {
    const { selectedType,
        pageIndex,
        pageSize,
        search,
        updateParams,
        handleTypeChange,
        handleSearch } = useProductsSearchParams();
    const { products: productsService } = useApi();
    const { t } = useTranslation();
    const [tick, setTick] = useState(0);
    const [showForm, setShowForm] = useState(false);
    const [editing, setEditing] = useState<Product | null>(null);
    const [deleting, setDeleting] = useState<Product | null>(null);

    const cacheKey = `admin-products-${pageIndex}-${pageSize}-${selectedType}-${search}-${tick}`;

    const { data: productPage, isLoading, error, reload } = useFetch<PaginatedItems<Product>>(
        cacheKey,
        async () => {
            const result = await productsService.fetchProducts(pageIndex, pageSize, selectedType, search);
            if (result.isError || !result.data) throw new Error(result.errorMessage ?? 'Failed to load products.');
            return result.data;
        }
    );

    const { data: catalogTypes } = useFetch<CatalogType[]>(
        CACHE_KEYS.CATALOG_TYPES,
        async () => {
            const result = await productsService.fetchCatalogTypes();
            if (result.isError || !result.data) throw new Error('Failed to load categories.');
            return result.data;
        }
    );

    const allProducts = productPage?.data ?? [];
    const totalCount = productPage?.count ?? 0;
    const totalPages = Math.ceil(totalCount / pageSize);

    const handleSaved = () => {
        setShowForm(false);
        setEditing(null);
        setTick(t => t + 1);
        reload();
    };

    const handleDeleted = () => {
        setDeleting(null);
        setTick(t => t + 1);
        reload();
    };

    const openCreate = () => { setEditing(null); setShowForm(true); };
    const openEdit = (p: Product) => { setEditing(p); setShowForm(true); };

    return (
        <div className="admin-page">
            <PageMetadata title={`${t('admin.title')} | Mango Admin`} description="Admin panel for managing products." />

            <div className="admin-page__header">
                <div>
                    <h1 className="admin-page__title">{t('admin.title')}</h1>
                    <p className="admin-page__subtitle">{totalCount} product{totalCount !== 1 ? 's' : ''} total</p>
                </div>
                <button className="btn-primary" onClick={openCreate}>+ {t('admin.createProduct')}</button>
            </div>

            <div className="admin-page__toolbar">
                <SearchBox
                    placeholder={t('products.searchPlaceholder')}
                    value={search}
                    onChange={handleSearch}
                    className="admin-search-wrapper"
                />

                <SelectBox
                    className="admin-filter"
                    value={selectedType ?? ''}
                    onChange={e => handleTypeChange(e.target.value ? Number(e.target.value) : undefined)}
                >
                    <option value="">{t('common.all') || "All Categories"}</option>
                    {catalogTypes?.map(t => (
                        <option key={t.id} value={t.id}>{t.type}</option>
                    ))}
                </SelectBox>
            </div>

            {isLoading && (
                <div className="admin-table-skeleton">
                    {Array.from({ length: pageSize }).map((_, i) => (
                        <div key={i} className="skeleton-row" />
                    ))}
                </div>
            )}

            {!isLoading && error && (
                <div className="admin-error">
                    <span>⚠️</span>
                    <p>{error}</p>
                    <button className="btn-secondary" onClick={reload}>{t('common.retry') || "Retry"}</button>
                </div>
            )}

            {!isLoading && !error && (
                <>
                    <div className="admin-table-wrapper">
                        <table className="admin-table">
                            <thead>
                                <tr>
                                    <th>Image</th>
                                    <th>{t('products.title')}</th>
                                    <th>{t('products.category')}</th>
                                    <th className="text-right">{t('products.price')}</th>
                                    <th className="text-right">Stock</th>
                                    <th className="text-center">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {allProducts.length === 0 ? (
                                    <tr>
                                        <td colSpan={6} className="admin-table__empty">{t('products.noProducts')}</td>
                                    </tr>
                                ) : (
                                    allProducts.map((p: Product) => {
                                        const placeholder = `https://placehold.co/48x48/1e1e2e/a6adc8?text=${encodeURIComponent(p.name[0])}`;
                                        return (
                                            <tr key={p.id}>
                                                <td>
                                                    <img
                                                        src={p.imageUrl || placeholder}
                                                        alt={p.name}
                                                        className="admin-table__thumb"
                                                        onError={e => { (e.target as HTMLImageElement).src = placeholder; }}
                                                    />
                                                </td>
                                                <td className="admin-table__name">{p.name}</td>
                                                <td>
                                                    <span className="badge-tag">{p.categoryName ?? '—'}</span>
                                                </td>
                                                <td className="text-right">${p.price.toFixed(2)}</td>
                                                <td className="text-right">
                                                    <span className={`badge-tag ${p.stock > 0 ? 'badge-tag--success' : 'badge-tag--danger'}`}>
                                                        {p.stock}
                                                    </span>
                                                </td>
                                                <td className="text-center">
                                                    <div className="admin-table__actions">
                                                        <button className="action-btn action-btn--edit" onClick={() => openEdit(p)}>✏️ {t('common.edit')}</button>
                                                        <button className="action-btn action-btn--delete" onClick={() => setDeleting(p)}>🗑 {t('common.delete')}</button>
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })
                                )}
                            </tbody>
                        </table>
                    </div>

                    <Pagination
                        currentPage={pageIndex}
                        totalPages={totalPages}
                        onPageChange={(page) => updateParams({ page })}
                        pageSize={pageSize}
                        onPageSizeChange={(size) => updateParams({ size, page: 1 })}
                        pageSizeOptions={PAGE_SIZE_OPTIONS}
                        className="admin-pagination"
                    />
                </>
            )}

            {showForm && (
                <ProductFormModal
                    editing={editing}
                    catalogTypes={catalogTypes ?? []}
                    onClose={() => { setShowForm(false); setEditing(null); }}
                    onSaved={handleSaved}
                />
            )}

            {deleting && (
                <DeleteDialog
                    product={deleting}
                    onCancel={() => setDeleting(null)}
                    onDeleted={handleDeleted}
                />
            )}
        </div>
    );
}
