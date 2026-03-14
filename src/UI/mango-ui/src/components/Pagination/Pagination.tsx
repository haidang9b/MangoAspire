import { PAGE_SIZE_OPTIONS } from '@/constants';
import { useTranslation } from 'react-i18next';
import './Pagination.css';

interface PaginationProps {
    currentPage: number;
    totalPages: number;
    onPageChange: (page: number) => void;
    pageSize?: number;
    onPageSizeChange?: (size: number) => void;
    pageSizeOptions?: number[];
    className?: string;
}

export const Pagination = ({
    currentPage,
    totalPages,
    onPageChange,
    pageSize,
    onPageSizeChange,
    pageSizeOptions = PAGE_SIZE_OPTIONS,
    className = ''
}: PaginationProps) => {
    const { t } = useTranslation();

    if (totalPages === 0 && !onPageSizeChange) return null;

    // Ensure current pageSize is in the options list
    const options = [...new Set([...pageSizeOptions, pageSize].filter((s): s is number => s !== undefined))].sort((a, b) => a - b);

    return (
        <nav className={`pagination ${className}`}>
            <div className="pagination__controls">
                <button
                    className="pagination-btn"
                    disabled={currentPage <= 1}
                    onClick={() => onPageChange(currentPage - 1)}
                >
                    ← {t('common.previous')}
                </button>
                <span className="pagination-info">
                    {t('common.page')} {currentPage} {t('common.of')} {totalPages || 1}
                </span>
                <button
                    className="pagination-btn"
                    disabled={currentPage >= totalPages || totalPages === 0}
                    onClick={() => onPageChange(currentPage + 1)}
                >
                    {t('common.next')} →
                </button>
            </div>

            {onPageSizeChange && (
                <div className="pagination__size-selector">
                    <label htmlFor="page-size">Show:</label>
                    <select
                        id="page-size"
                        value={pageSize}
                        onChange={(e) => onPageSizeChange(Number(e.target.value))}
                    >
                        {options.map(size => (
                            <option key={size} value={size}>{size}</option>
                        ))}
                    </select>
                </div>
            )}
        </nav>
    );
};
