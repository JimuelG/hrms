export const peso = (n: number): string =>
    new Intl.NumberFormat(undefined, { style: 'currency', currency: 'PHP', minimumFractionDigits: 2}).format(n);

export function dateLabel(iso: string): string {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}