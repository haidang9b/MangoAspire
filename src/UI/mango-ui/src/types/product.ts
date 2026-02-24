export interface CatalogType {
    id: number;
    type: string;
}

export interface Product {
    id: string;
    name: string;
    price: number;
    description: string;
    imageUrl: string;
    catalogTypeId: number;
    categoryName?: string;
    catalogType?: CatalogType;
    stock: number;
}
