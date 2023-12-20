declare global {
    interface String {
        format(object: Record<string, string>): string
        isSrt(): boolean
    }
}

/**
 * Formats a string by replacing placeholders with values from the provided object.
 *
 * @param {Record<string, string>} object - An object containing key-value pairs to replace placeholders in the string.
 * @returns {string} The formatted string with replaced placeholders.
 * @memberof String
 * @function format
 */
String.prototype.format = function (object: Record<string, string>) {
    let string = this as string,
        key

    for (key in object)
        string = string.replace(new RegExp('\\{{' + key + '\\}}', 'gm'), object[key])

    return string
}

/**
 * Checks if a string represents a SubRip subtitle file based on its file extension.
 *
 * @returns {boolean} Returns true if the string has a ".srt" extension (case-insensitive), otherwise false.
 * @memberof String
 * @function isSrt
 */
String.prototype.isSrt = function () {
    const srtRegex = /\.srt\b/i
    return srtRegex.test(this)
}

export {}
