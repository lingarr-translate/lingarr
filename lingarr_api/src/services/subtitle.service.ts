import { parseSync, stringifySync } from 'subtitle'
import { NodeList } from 'subtitle'

class SubtitleService {
    /**
     * Parses a subtitle file and returns a NodeList of subtitle objects.
     *
     * @param {string} subtitleFile - The content of the subtitle file to be parsed.
     * @returns {Promise<NodeList>} A promise that resolves to a NodeList of subtitle objects.
     *
     * {
     *   type: 'cue',
     *   data: {
     *     start: 75300,
     *     end: 77433,
     *     text: 'lingarr\n turbocharged subtitle translation'
     *   }
     * }
     */
    async parse(subtitleFile: string): Promise<NodeList> {
        return parseSync(subtitleFile)
    }

    /**
     * Converts a NodeList of subtitle objects to a string in SRT format.
     *
     * @param {NodeList} subtitleFile - The NodeList of subtitle objects to be converted.
     * @returns {Promise<string>} A promise that resolves to the converted subtitle file as a string in SRT format.
     */
    async convert(subtitleFile: NodeList): Promise<string> {
        return stringifySync(subtitleFile, { format: 'SRT' })
    }

    /**
     * Asynchronously converts a formatted SRT file text into an array of subtitle nodes.
     *
     * @param {string} text - The content of the SRT file to be converted.
     * @returns {Promise<NodeList>} A promise that resolves to an array of subtitle nodes representing the content of the SRT file.
     * @throws {Error} Throws an error if the provided text is not a valid SRT file.
     */
    async toArray(text: string): Promise<NodeList> {
        if (!this.isValid(text)) {
            throw new Error('Cannot convert to a valid SRT file')
        }
        return this.parse(text)
    }

    /**
     * Checks whether the provided content is a valid SRT file based on its structure.
     *
     * @param {string} content - The content to be checked for SRT file validity.
     * @returns {boolean} True if the content is a valid SRT file, false otherwise.
     */
    isValid(content: string): boolean {
        const subtitle = content.indexOf('\n')
        const r = /^\s*(\d+:\d+:\d+,\d+)[^\S\n]+-->[^\S\n]+(\d+:\d+:\d+,\d+)/
        return r.test(content.slice(subtitle + 1, 50))
    }
}

export default new SubtitleService()
