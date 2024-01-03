interface Array<T> {
    search(key: string, filter: string): Array<T>
}

/**
 * Extends the Array prototype with a custom 'search' method.
 *
 * @this {Array<any>} The array to be filtered.
 * @param {string} key - The key to use for filtering.
 * @param {string} value - The value to search for.
 * @returns {Array<any>} - The filtered array.
 */
Array.prototype.search = function <T>(this: Array<T>, key: keyof T, value: string): Array<T> {
    return this.filter((item) => {
        const search = value
            // Normalize the string by decomposing accented characters into their base form (NFD)
            .normalize('NFD')
            // Replace hyphens with spaces
            .replace(/-/g, ' ')
            // Remove diacritic marks from characters
            .replace(/[\u0300-\u036f]/g, '')
            // Remove non-word and non-space characters (excluding letters and spaces)
            .replace(/[^\w\s]/gi, '')
            .toLowerCase()

        return item[key]
            ?.toString()
            .toLowerCase()
            .normalize('NFD')
            .replace(/-/g, ' ')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/[^\w\s]/gi, '')
            .replace(/  +/g, ' ')
            .includes(search)
    })
}
