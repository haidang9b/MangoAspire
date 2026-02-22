import { useEffect } from 'react';

interface PageMetadataProps {
    title: string;
    description?: string;
}

export function PageMetadata({ title, description }: PageMetadataProps) {
    useEffect(() => {
        // Update document title
        document.title = title;

        // Update meta description
        if (description) {
            let metaDescription = document.querySelector('meta[name="description"]');
            if (!metaDescription) {
                metaDescription = document.createElement('meta');
                metaDescription.setAttribute('name', 'description');
                document.head.appendChild(metaDescription);
            }
            metaDescription.setAttribute('content', description);
        }
    }, [title, description]);

    return null; // This component doesn't render anything to the DOM
}
