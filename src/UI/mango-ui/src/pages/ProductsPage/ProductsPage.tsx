import { useApi, useFetch, useProducts, useProductsSearchParams } from '../../hooks';
import { ProductCard, SearchBox, PageMetadata } from '../../components';
import type { CatalogType } from '../../types';
import './ProductsPage.css';

export function ProductsPage() {
    const { products: productsService } = useApi();
    const { selectedType,
        pageIndex,
        pageSize,
        search,
        updateParams,
        handleTypeChange,
        handleSearch } = useProductsSearchParams();

    const { products, totalCount, isLoading, error, reload } = useProducts({
        pageIndex,
        pageSize,
        catalogTypeId: selectedType,
        search,
    });

    const { data: catalogTypes } = useFetch<CatalogType[]>(
        'catalog-types',
        async () => {
            const result = await productsService.fetchCatalogTypes();
            if (result.isError || !result.data) throw new Error('Failed to load categories.');
            return result.data;
        }
    );

    const totalPages = Math.ceil(totalCount / pageSize);

    return (
        <div className="products-page">
            <PageMetadata
                title="Products | Mango Store"
                description="Explore our wide variety of fresh mangoes and tropical fruits."
            />
            {/* Controls */}
            <div className="products-page__controls">
                <div className="products-page__search-bar">
                    <SearchBox
                        placeholder="Search products..."
                        value={search}
                        onChange={handleSearch}
                    />
                </div>
                <div className="products-page__filter-group">
                    <button
                        className={`filter-chip ${selectedType == null ? 'filter-chip--active' : ''}`}
                        onClick={() => handleTypeChange(undefined)}
                    >
                        All
                    </button>
                    {(catalogTypes ?? []).map((ct) => (
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
                        disabled={pageIndex <= 1}
                        onClick={() => updateParams({ page: pageIndex - 1 })}
                    >
                        ← Prev
                    </button>
                    <span className="pagination-info">
                        Page {pageIndex} of {totalPages}
                    </span>
                    <button
                        className="pagination-btn"
                        disabled={pageIndex >= totalPages}
                        onClick={() => updateParams({ page: pageIndex + 1 })}
                    >
                        Next →
                    </button>
                </nav>
            )}
        </div>
    );
}
