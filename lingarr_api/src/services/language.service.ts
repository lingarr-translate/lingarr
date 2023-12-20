import fsp from 'fs/promises'
import { ILanguage } from '../ts/language'
import path from 'path'
import fs from 'fs'

class LanguageService {
    /**
     * Retrieves the list of languages from a file.
     *
     * @returns {Promise<ILanguage[]>} A promise that resolves to an array of language objects.
     * @throws {Error} If there is an error reading or parsing the file.
     */
    async languages(): Promise<ILanguage[]> {
        try {
            const languageConfigPath = path.resolve(__dirname, '../../config/languages.json')
            const languagePath = path.resolve(__dirname, '../statics/languages.json')

            // if no custom config for languages is provided we use the default
            const fileContent = fs.existsSync(languageConfigPath)
                ? await fsp.readFile(languageConfigPath, 'utf-8')
                : await fsp.readFile(languagePath, 'utf-8')

            return JSON.parse(fileContent) || []
        } catch (error) {
            console.error(error)
            return []
        }
    }

    /**
     * Validates if the provided language code exists in the list of supported languages.
     *
     * @param {string} language - The language code to validate.
     * @returns {Promise<boolean>} A promise that resolves to true if the language is valid, false otherwise.
     */
    async validate(language: string) {
        try {
            const supportedLanguages = await this.languages()
            return supportedLanguages.some((lang) => lang.name === language)
        } catch (error) {
            console.error(error)
            return false
        }
    }
}

export default new LanguageService()
