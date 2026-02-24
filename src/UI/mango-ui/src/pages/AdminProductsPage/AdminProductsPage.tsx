import { useState } from 'react';
import { useApi } from '../../hooks/useApi';
import { useFetch } from '../../hooks/useFetch';
import { PageMetadata, SearchBox, SelectBox, Pagination } from '../../components';
import { ProductFormModal } from './ProductFormModal';
import { DeleteDialog } from './DeleteDialog';
import type { Product, CatalogType } from '../../types/product';
import type { PaginatedItems } from '../../types/api';
import { useProductsSearchParams } from '../../hooks';

import './AdminProductsPage.css';

const CACHE_TYPES = 'catalog-types';

export function AdminProductsPage() {
    const { selectedType,
        pageIndex,
        pageSize,
        search,
        updateParams,
        handleTypeChange,
        handleSearch } = useProductsSearchParams();
    const { products: productsService } = useApi();
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
        CACHE_TYPES,
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
            <PageMetadata title="Manage Products | Mango Admin" description="Admin panel for managing products." />

            <div className="admin-page__header">
                <div>
                    <h1 className="admin-page__title">Product Management</h1>
                    <p className="admin-page__subtitle">{totalCount} product{totalCount !== 1 ? 's' : ''} total</p>
                </div>
                <button className="btn-primary" onClick={openCreate}>+ New Product</button>
            </div>

            <div className="admin-page__toolbar">
                <SearchBox
                    placeholder="Search by name…"
                    value={search}
                    onChange={handleSearch}
                    className="admin-search-wrapper"
                />

                <SelectBox
                    className="admin-filter"
                    value={selectedType ?? ''}
                    onChange={e => handleTypeChange(e.target.value ? Number(e.target.value) : undefined)}
                >
                    <option value="">All Categories</option>
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
                    <button className="btn-secondary" onClick={reload}>Retry</button>
                </div>
            )}

            {!isLoading && !error && (
                <>
                    <div className="admin-table-wrapper">
                        <table className="admin-table">
                            <thead>
                                <tr>
                                    <th>Image</th>
                                    <th>Name</th>
                                    <th>Category</th>
                                    <th className="text-right">Price</th>
                                    <th className="text-right">Stock</th>
                                    <th className="text-center">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {allProducts.length === 0 ? (
                                    <tr>
                                        <td colSpan={6} className="admin-table__empty">No products found.</td>
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
                                                        <button className="action-btn action-btn--edit" onClick={() => openEdit(p)}>✏️ Edit</button>
                                                        <button className="action-btn action-btn--delete" onClick={() => setDeleting(p)}>🗑 Delete</button>
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
                        pageSizeOptions={[10, 20, 50]}
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
