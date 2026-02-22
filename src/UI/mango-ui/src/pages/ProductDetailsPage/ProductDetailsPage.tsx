import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useApi } from '../../hooks/useApi';
import { useFetch } from '../../hooks/useFetch';
import { useAuth } from '../../auth/AuthContext';
import { useCart } from '../../context/CartContext';
import { PageMetadata } from '../../components/PageMetadata';
import type { Product } from '../../types/product';
import './ProductDetailsPage.css';

export function ProductDetailsPage() {
    const { products: productsService } = useApi();
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { user, login } = useAuth();
    const { addToCart } = useCart();

    const [quantity, setQuantity] = useState(1);
    const [adding, setAdding] = useState(false);

    const { data: product, isLoading, error } = useFetch<Product>(
        `product-${id}`,
        async () => {
            const result = await productsService.fetchProductById(id!);
            if (result.isError || !result.data) throw new Error(result.errorMessage ?? 'Product not found.');
            return result.data;
        },
        { enabled: !!id }
    );

    const handleAddToCart = async () => {
        if (!product) return;
        if (!user) { login(); return; }
        setAdding(true);
        await addToCart(product.id, quantity);
        setAdding(false);
    };

    if (isLoading) {
        return (
            <div className="product-details-page product-details-page--loading">
                <p>Loading product details...</p>
            </div>
        );
    }

    if (error || !product) {
        return (
            <div className="product-details-page product-details-page--error">
                <p>⚠️ {error || 'Product not found'}</p>
                <button onClick={() => navigate('/')}>Back to Shop</button>
            </div>
        );
    }

    const placeholder = `https://placehold.co/600x400/1e1e2e/a6adc8?text=${encodeURIComponent(product.name)}`;

    return (
        <div className="product-details-page">
            <PageMetadata
                title={`${product.name} | Mango Store`}
                description={product.description?.replace(/<[^>]*>/g, '').slice(0, 160) || `Buy ${product.name} at Mango Store.`}
            />
            <div className="product-details__back">
                <Link to="/" className="back-link">← Back to Products</Link>
            </div>

            <div className="product-details__container">
                <div className="product-details__image-section">
                    <img
                        src={product.imageUrl || placeholder}
                        alt={product.name}
                        className="product-details__image"
                        onError={(e) => { (e.target as HTMLImageElement).src = placeholder; }}
                    />
                </div>

                <div className="product-details__info-section">
                    <div className="product-details__info">
                        <div className="product-details__header-row">
                            <h1 className="product-details__name">{product.name}</h1>
                            <span className="product-details__price-lg">${product.price.toFixed(2)}</span>
                        </div>

                        <div className="product-details__meta">
                            <span className="badge-tag">{product.catalogType?.type ?? 'Mango'}</span>
                            {product.stock > 0 ? (
                                <span className="badge-tag badge-tag--success">In Stock: {product.stock}</span>
                            ) : (
                                <span className="badge-tag badge-tag--danger">Out of Stock</span>
                            )}
                        </div>

                        <div className="product-details__description" dangerouslySetInnerHTML={{ __html: product.description }} />

                        <div className="product-details__action-box">
                            <div className="quantity-selector">
                                <label htmlFor="quantity">Quantity</label>
                                <div className="quantity-controls">
                                    <button type="button" onClick={() => setQuantity(q => Math.max(1, q - 1))}>−</button>
                                    <input
                                        id="quantity"
                                        type="number"
                                        value={quantity}
                                        onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                                        min="1"
                                    />
                                    <button type="button" onClick={() => setQuantity(q => q + 1)}>+</button>
                                </div>
                            </div>

                            <div className="product-details__btns">
                                <Link to="/" className="btn-secondary">Back to List</Link>
                                <button
                                    className="btn-primary"
                                    onClick={handleAddToCart}
                                    disabled={adding || product.stock <= 0}
                                >
                                    {adding ? 'Adding...' : 'Add to Cart'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
