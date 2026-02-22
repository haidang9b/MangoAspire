import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useApi } from '../../hooks/useApi';
import { useProducts } from '../../hooks/useProducts';
import { ProductCard } from '../../components/ProductCard';
import { PageMetadata } from '../../components/PageMetadata';
import type { CatalogType } from '../../types/product';
import './ProductsPage.css';

const DEFAULT_PAGE_SIZE = 12;

export function ProductsPage() {
    const { products: productsService } = useApi();
    const [searchParams, setSearchParams] = useSearchParams();
    const [catalogTypes, setCatalogTypes] = useState<CatalogType[]>([]);

    // State derived from URL
    const selectedType = searchParams.get('type') ? Number(searchParams.get('type')) : undefined;
    const pageIndex = Math.max(0, (Number(searchParams.get('page')) || 1) - 1);
    const pageSize = Number(searchParams.get('size')) || DEFAULT_PAGE_SIZE;

    const { products, totalCount, isLoading, error, reload } = useProducts({
        pageIndex,
        pageSize,
        catalogTypeId: selectedType,
    });

    useEffect(() => {
        productsService.fetchCatalogTypes()
            .then((r) => { if (!r.isError && r.data) setCatalogTypes(r.data); })
            .catch(() => {/* non-fatal */ });
    }, [productsService]);

    const totalPages = Math.ceil(totalCount / pageSize);

    const updateParams = (updates: Record<string, string | number | undefined>) => {
        setSearchParams(prev => {
            const next = new URLSearchParams(prev);
            Object.entries(updates).forEach(([key, value]) => {
                if (value === undefined || value === '' || (key === 'page' && value === 1) || (key === 'size' && value === DEFAULT_PAGE_SIZE)) {
                    next.delete(key);
                } else {
                    next.set(key, String(value));
                }
            });
            return next;
        }, { replace: true });
    };

    const handleTypeChange = (typeId?: number) => {
        updateParams({ type: typeId, page: 1 });
    };

    return (
        <div className="products-page">
            <PageMetadata
                title="Products | Mango Store"
                description="Explore our wide variety of fresh mangoes and tropical fruits."
            />
            {/* Controls */}
            <div className="products-page__controls">
                <div className="products-page__filter-group">
                    <button
                        className={`filter-chip ${selectedType == null ? 'filter-chip--active' : ''}`}
                        onClick={() => handleTypeChange(undefined)}
                    >
                        All
                    </button>
                    {catalogTypes.map((ct) => (
                        <button
                            key={ct.id}
                            className={`filter-chip ${selectedType === ct.id ? 'filter-chip--active' : ''}`}
                            onClick={() => handleTypeChange(ct.id)}
                        >
                            {ct.type}
                        </button>
                    ))}
                </div>
                <p className="products-page__count">
                    {isLoading ? 'Loading…' : `${totalCount} product${totalCount !== 1 ? 's' : ''}`}
                </p>
            </div>

            {/* Content */}
            <main className="products-page__content">
                {isLoading && (
                    <div className="products-page__skeleton-grid">
                        {Array.from({ length: 8 }).map((_, i) => (
                            <div key={i} className="skeleton-card" />
                        ))}
                    </div>
                )}

                {!isLoading && error && (
                    <div className="products-page__error">
                        <span>⚠️</span>
                        <p>{error}</p>
                        <button onClick={reload}>Retry</button>
                    </div>
                )}

                {!isLoading && !error && products.length === 0 && (
                    <div className="products-page__empty">
                        <span>🔍</span>
                        <p>No products found.</p>
                    </div>
                )}

                {!isLoading && !error && products.length > 0 && (
                    <div className="products-page__grid">
                        {products.map((p) => (
                            <ProductCard key={p.id} product={p} />
                        ))}
                    </div>
                )}
            </main>

            {/* Pagination */}
            {totalPages > 1 && (
                <nav className="products-page__pagination">
                    <button
                        className="pagination-btn"
                        disabled={pageIndex === 0}
                        onClick={() => updateParams({ page: pageIndex })}
                    >
                        ← Prev
                    </button>
                    <span className="pagination-info">
                        Page {pageIndex + 1} of {totalPages}
                    </span>
                    <button
                        className="pagination-btn"
                        disabled={pageIndex >= totalPages - 1}
                        onClick={() => updateParams({ page: pageIndex + 2 })}
                    >
                        Next →
                    </button>
                </nav>
            )}
        </div>
    );
}
