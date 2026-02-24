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
    pageSizeOptions = [10, 20, 50],
    className = ''
}: PaginationProps) => {
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
                    ← Prev
                </button>
                <span className="pagination-info">
                    Page {currentPage} of {totalPages || 1}
                </span>
                <button
                    className="pagination-btn"
                    disabled={currentPage >= totalPages || totalPages === 0}
                    onClick={() => onPageChange(currentPage + 1)}
                >
                    Next →
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
