export const formatDate = (dateString: string) => {
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    }

    const locale = navigator.language || 'en-US'
    return new Date(dateString).toLocaleDateString(locale, options)
}
