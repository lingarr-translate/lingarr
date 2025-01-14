/**
 * Formats a date string into a more readable format.
 * @example
 * formatDate("2025-01-01") // "1 Jan 2025"
 */
export const formatDate = (dateString: string | Date) => {
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    }

    const locale = navigator.language || 'en-US'
    return new Date(dateString).toLocaleDateString(locale, options)
}

/**
 * Formats a date string into a more readable format.
 * @example
 * formatDate("2025-01-01") // "1 Jan 2025"
 */
export const formatDateTime = (dateString: string | Date) => {
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: 'numeric'
    }

    const locale = navigator.language || 'en-US'
    return new Date(dateString).toLocaleDateString(locale, options)
}
