import { NodeList } from 'subtitle'

class TranslateService {
    /**
     * Asynchronously translates the original subtitles to the specified target language, combines them with the original structure.
     *
     * @param {NodeList} parsedSubtitles - The original list of subtitles to be translated.
     * @param {string} targetLanguage - The target language code for translation.
     * @returns {Promise<NodeList>} A promise that resolves to the translated subtitles.
     * @throws {Error} Throws an error if any step of the translation or combination process fails.
     */
    async translate(subtitles: NodeList, targetLanguage: string): Promise<NodeList> {
        const LibreTranslateApi = process.env.LIBRETRANSLATE_API
            ? process.env.LIBRETRANSLATE_API
            : 'http://libretranslate:5000'
        try {
            for (const subtitle of subtitles) {
                if (subtitle.type === 'cue') {
                    const response = await fetch(`${LibreTranslateApi}/translate`, {
                        method: 'POST',
                        body: JSON.stringify({
                            q: subtitle.data.text,
                            source: 'auto',
                            target: targetLanguage,
                            format: 'text'
                        }),
                        headers: { 'Content-Type': 'application/json' }
                    })

                    const result = await response.json()
                    subtitle.data.text = result.translatedText
                }
            }
            return subtitles
        } catch (error) {
            console.error(error)
            console.error('Error sending request to LibreTranslate API:', error.message)
        }
    }
}

export default new TranslateService()
