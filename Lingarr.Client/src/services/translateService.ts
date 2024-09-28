import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ILanguage, ISubtitle, ITranslateService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/translate'): ITranslateService => ({
    translateSubtitle<T>(subtitle: ISubtitle, source: string, target: ILanguage): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(resource, {
                subtitlePath: subtitle.path,
                sourceLanguage: source,
                targetLanguage: target.code
            })

                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const translateService = (axios: AxiosStatic): ITranslateService => {
    return service(axios)
}
