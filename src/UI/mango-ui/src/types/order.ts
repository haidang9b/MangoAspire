export interface OrderDto {
    id: string;
    orderTime: string;
    orderTotal: number;
    status: string;
    itemCount: number;
}

export interface OrderDetailDto {
    id: string;
    orderTime: string;
    pickupDate: string;
    orderTotal: number;
    discountTotal: number;
    status: string;
    paymentStatus: boolean;
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    couponCode?: string;
    cancelReason?: string;
    items: OrderItemDto[];
}

export interface OrderItemDto {
    id: string;
    productId: string;
    productName: string;
    price: number;
    count: number;
}
