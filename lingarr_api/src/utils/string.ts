declare global {
    interface String {
        format(object: Record<string, string>): string
    }
}

/**
 * Formats a string by replacing placeholders with values from the provided object.
 *
 * @param {Record<string, string>} object - An object containing key-value pairs to replace placeholders in the string.
 * @returns {string} The formatted string with replaced placeholders.
 *
 * @example
 * const templateString = 'Hello, {{name}}! Today is {{day}}.';
 * const values = { name: 'John', day: 'Monday' };
 * const formattedString = templateString.format(values);
 * console.log(formattedString); // Output: 'Hello, John! Today is Monday.'
 */
String.prototype.format = function (object: Record<string, string>) {
    let string = this as string,
        key

    for (key in object)
        string = string.replace(new RegExp('\\{{' + key + '\\}}', 'gm'), object[key])

    return string
}

export {}
