import { Link } from 'react-router-dom';
import type { Product } from '../types/product';
import './ProductCard.css';

interface Props {
    product: Product;
}

export function ProductCard({ product }: Props) {
    const placeholder = `https://placehold.co/400x280/1e1e2e/a6adc8?text=${encodeURIComponent(product.name)}`;
    const imgSrc = product.imageUrl || placeholder;

    return (
        <article className="product-card">
            <div className="product-card__image-container">
                <img
                    src={imgSrc}
                    alt={product.name}
                    className="product-card__img"
                    onError={(e) => { (e.target as HTMLImageElement).src = placeholder; }}
                />
            </div>
            <div className="product-card__body">
                <h3 className="product-card__title text-success">{product.name}</h3>
                <span className="badge-tag mb-2">{product.catalogType?.type ?? 'Mango'}</span>
                <div
                    className="product-card__description"
                    dangerouslySetInnerHTML={{ __html: product.description }}
                />

                <div className="product-card__actions-row">
                    <div className="product-card__price-col">
                        <span className="product-card__price-text">
                            ${product.price.toFixed(2)}
                        </span>
                    </div>
                    <div className="product-card__btn-col">
                        <Link
                            to={`/product/${product.id}`}
                            className="btn-secondary w-full"
                            style={{ fontSize: '0.9rem', height: '40px', padding: '0 12px' }}
                        >
                            Details
                        </Link>
                    </div>
                </div>
            </div>
        </article>
    );
}
