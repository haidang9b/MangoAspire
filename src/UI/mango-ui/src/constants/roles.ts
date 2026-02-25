export const Roles = {
    Admin: 'admin',
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];

/**
 * Case-insensitive role check.
 * Works whether the identity server returns role as a string or an array.
 */
export function hasRole(role: string | string[] | undefined, target: string): boolean {
    if (!role) return false;
    const t = target.toLowerCase();
    if (Array.isArray(role)) return role.some(r => r.toLowerCase() === t);
    return role.toLowerCase() === t;
}
