import { useEffect, useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useProducts } from '../hooks/useProducts';
import { ProductCard } from '../components/ProductCard';
import type { CatalogType } from '../types/product';
import './ProductsPage.css';

const PAGE_SIZE = 12;

export function ProductsPage() {
    const { products: productsService } = useApi();
    const [catalogTypes, setCatalogTypes] = useState<CatalogType[]>([]);
    const [selectedType, setSelectedType] = useState<number | undefined>();
    const [pageIndex, setPageIndex] = useState(0);

    const { products, totalCount, isLoading, error, reload } = useProducts({
        pageIndex,
        pageSize: PAGE_SIZE,
        catalogTypeId: selectedType,
    });

    useEffect(() => {
        productsService.fetchCatalogTypes()
            .then((r) => { if (!r.isError && r.data) setCatalogTypes(r.data); })
            .catch(() => {/* non-fatal */ });
    }, [productsService]);

    const totalPages = Math.ceil(totalCount / PAGE_SIZE);

    const handleTypeChange = (typeId?: number) => {
        setSelectedType(typeId);
        setPageIndex(0);
    };

    return (
        <div className="products-page">
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
                        onClick={() => setPageIndex((p) => p - 1)}
                    >
                        ← Prev
                    </button>
                    <span className="pagination-info">
                        Page {pageIndex + 1} of {totalPages}
                    </span>
                    <button
                        className="pagination-btn"
                        disabled={pageIndex >= totalPages - 1}
                        onClick={() => setPageIndex((p) => p + 1)}
                    >
                        Next →
                    </button>
                </nav>
            )}
        </div>
    );
}
