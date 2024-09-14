interface String {
    /**
     * Adds query parameters to the string URL.
     *
     * This method appends the provided key-value pairs to the query string of the URL.
     * If the URL already contains a query string, new parameters are added to it.
     *
     * @param params An object where each key-value pair represents a query parameter.
     * @returns The string URL with the updated query parameters.
     *
     * @example
     * const baseUrl = "https://api.example.com/translate";
     * const subtitlePath = "path/to/subtitle.srt";
     * const sourceLanguage = "en";
     * const targetLanguage = "es";
     *
     * // Use the addParams method to append query parameters
     * const url = baseUrl.addParams({
     *   subtitle: subtitlePath,
     *   source: sourceLanguage,
     *   target: targetLanguage,
     * });
     *
     * console.log(url);
     * // Output: "https://api.example.com/translate?subtitle=path%2Fto%2Fsubtitle.srt&source=en&target=es"
     */
    addParams(params: {
        [key: string]: string | number | boolean | string[] | number[] | Record<string, any>
    }): string

    /**
     * Formats a string by replacing placeholders e.g. `{key}` with values from the given object.
     *
     * @param {object} params - The object containing key-value pairs for replacement.
     * @returns {string} The formatted string.
     */
    format(params: Record<string, string | number>): string
}

String.prototype.addParams = function (params: {
    [key: string]: string | number | boolean | string[] | number[] | Record<string, any>
}): string {
    const urlString = this.toString()

    const hasQueryString = urlString.includes('?')

    const queryString = Object.keys(params)
        .flatMap((key) => {
            const value = params[key]
            if (Array.isArray(value)) {
                // For array values
                return value.map((v) => `${encodeURIComponent(key)}=${encodeURIComponent(v)}`)
            } else if (typeof value === 'object' && value !== null) {
                // For objects, serialize to JSON
                return `${encodeURIComponent(key)}=${encodeURIComponent(JSON.stringify(value))}`
            } else {
                // For single values, return one entry
                return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
            }
        })
        .join('&')

    if (hasQueryString) {
        return `${urlString}&${queryString}`
    } else {
        return `${urlString}?${queryString}`
    }
}

String.prototype.format = function (params: Record<string, string | number>): string {
    let string = this as string
    for (const key in params) {
        const value = params[key]
        string = string.replace(new RegExp('\\{' + key + '\\}', 'gm'), value.toString())
    }
    return string
}
