import './SearchBox.css';

interface SearchBoxProps {
    value: string;
    placeholder?: string;
    onChange: (value: string) => void;
    className?: string;
}

export const SearchBox = ({ value, onChange, placeholder, className = '' }: SearchBoxProps) => {
    return (
        <div className={`search-box ${className}`}>
            <span className="search-box__icon">🔍</span>
            <input
                type="text"
                placeholder={placeholder}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                className="search-box__input"
            />
        </div>
    );
};
