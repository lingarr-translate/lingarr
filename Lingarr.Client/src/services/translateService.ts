import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ILanguage, IProofreadLineRequest, ISubtitle, ITranslateService, MediaType } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/translate'): ITranslateService => ({
    translateSubtitle<T>(
        mediaId: number,
        subtitle: ISubtitle,
        source: string,
        target: ILanguage,
        mediaType: MediaType
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/file`, {
                mediaId: mediaId,
                subtitlePath: subtitle.path,
                subtitleFormat: subtitle.format,
                sourceLanguage: source,
                targetLanguage: target.code,
                mediaType: mediaType
            })

                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    bulkTranslate<T>(
        mediaIds: number[],
        targetLanguage: string,
        mediaType: MediaType
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/bulk`, {
                mediaIds,
                targetLanguage,
                mediaType
            })
            .then((response: AxiosResponse<T>) => {
                resolve(response.data)
            })
            .catch((error: AxiosError) => {
                reject(error.response)
            })
        })
    },
    getLanguages<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/languages`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    proofreadStatus<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/proofread/status`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    proofreadLine<T>(request: IProofreadLineRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/proofread`, request)
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
