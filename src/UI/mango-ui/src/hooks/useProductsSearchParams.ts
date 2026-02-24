import { useSearchParams } from "react-router-dom";
import { DEFAULT_PAGE_SIZE } from '../constants';

export const useProductsSearchParams = () => {
    const [searchParams, setSearchParams] = useSearchParams();

    const selectedType = searchParams.get('type') ? Number(searchParams.get('type')) : undefined;
    const pageIndex = Math.max(1, Number(searchParams.get('page')) || 1);
    const pageSize = Number(searchParams.get('size')) || DEFAULT_PAGE_SIZE;
    const search = searchParams.get('search') || '';

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

    const handleSearch = (query: string) => {
        updateParams({ search: query, page: 1 });
    };

    return {
        selectedType,
        pageIndex,
        pageSize,
        search,
        updateParams,
        handleTypeChange,
        handleSearch,
    };
}