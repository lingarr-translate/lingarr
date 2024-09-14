import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ISubtitle, ITranslateService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/translate'): ITranslateService => ({
    translateSubtitle<T>(subtitle: ISubtitle, target: string): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(resource, {
                subtitlePath: subtitle.path,
                targetLanguage: target
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
