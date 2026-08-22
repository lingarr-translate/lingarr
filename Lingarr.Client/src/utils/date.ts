const timeZone =
    document.querySelector('meta[name="lingarr-timezone"]')?.getAttribute('content') || undefined

/**
 * Formats a date string into a more readable format.
 * @example
 * formatDate("2025-01-01") // "1 Jan 2025"
 */
export const formatDate = (dateString: string | Date) => {
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        timeZone
    }

    return new Date(dateString).toLocaleDateString(undefined, options)
}

/**
 * Formats a date string into a more readable format, including the time of day.
 * @example
 * formatDateTime("2025-01-01T13:45:00Z") // "1 Jan 2025, 13:45"
 */
export const formatDateTime = (dateString: string | Date) => {
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: 'numeric',
        timeZone
    }

    return new Date(dateString).toLocaleString(undefined, options)
}

/**
 * Formats a date string into the time of day alone.
 * @example
 * formatTime("2025-01-01T13:45:07Z") // "13:45:07"
 */
export const formatTime = (dateString: string | Date) => {
    const options: Intl.DateTimeFormatOptions = {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hourCycle: 'h23',
        timeZone
    }

    return new Date(dateString).toLocaleTimeString(undefined, options)
}

/**
 * Formats a date-only string into a short day label.
 * The value carries no time of day, so it is not converted between time zones.
 * @example
 * formatDayLabel("2025-01-01") // "1 Jan"
 */
export const formatDayLabel = (dateString: string) => {
    const options: Intl.DateTimeFormatOptions = {
        month: 'short',
        day: 'numeric'
    }

    return new Date(`${dateString}T00:00:00`).toLocaleDateString(undefined, options)
}
