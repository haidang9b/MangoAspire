import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { useCart } from '../context/CartContext';
import type { Product } from '../types/product';
import './ProductDetailsPage.css';

export function ProductDetailsPage() {
    const { products: productsService } = useApi();
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { addToCart } = useCart();

    const [product, setProduct] = useState<Product | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [quantity, setQuantity] = useState(1);
    const [adding, setAdding] = useState(false);

    useEffect(() => {
        if (!id) return;

        const loadProduct = async () => {
            setLoading(true);
            setError(null);
            try {
                const result = await productsService.fetchProductById(id);
                if (!result.isError && result.data) {
                    setProduct(result.data);
                } else {
                    setError(result.errorMessage ?? 'Product not found.');
                }
            } catch (err) {
                setError('Failed to load product details.');
            } finally {
                setLoading(false);
            }
        };

        loadProduct();
    }, [id]);

    const handleAddToCart = async () => {
        if (!product) return;
        setAdding(true);
        const success = await addToCart(product.id, quantity);
        setAdding(false);
        if (success) {
            // Optional: show a toast or message
        }
    };

    if (loading) {
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
