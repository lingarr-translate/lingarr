import { Request, Response } from 'express'
import LanguageService from '../services/language.service'
import TranslateService from '../services/translate.service'
import ResourceService from '../services/resource.service'
import SubtitleService from '../services/subtitle.service'

export default class TranslateController {
    async translate(request: Request, response: Response) {
        try {
            // Validate if the target language is supported
            if (!(await LanguageService.validate(request.body.targetLanguage))) {
                new Error('Selected language does not exist.')
            }

            // Read subtitles from the specified path
            const subtitles = await ResourceService.readSubtitle(request.body.path)

            // Parse the original subtitles into a structured format
            const parsedSubtitles = await SubtitleService.parse(subtitles)

            // Translate and write the subtitles to the specified path
            const combinedSubtitles = await TranslateService.translate(
                parsedSubtitles,
                request.body.targetLanguage
            )

            // Convert the combined subtitles to the output format
            const convertedSubtitles = await SubtitleService.convert(combinedSubtitles)

            // Write the translated and converted subtitles back to the specified path
            await ResourceService.writeSubtitle(
                request.body.path,
                request.body.targetLanguage,
                convertedSubtitles
            )

            console.info('Translation completed')
            response.status(200).json(['success'])
        } catch (err) {
            response.status(500).json(err)
        }
    }
}
