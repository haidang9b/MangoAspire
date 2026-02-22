export interface ResultModel<T> {
    data: T;
    isError: boolean;
    errorMessage?: string;
}

export interface PagedModel<T> {
    pageIndex: number;
    pageSize: number;
    count: number;
    data: T[];
}

// Alias for compatibility during refactoring
export type PaginatedItems<T> = PagedModel<T>;
