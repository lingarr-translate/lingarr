import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import {
    IBulkRemoveRequest,
    IProofreadLineApplyRequest,
    ITranslationRequest,
    ITranslationRequestService,
    TranslationStatus
} from '@/ts'

const service = (
    http: AxiosStatic,
    resource = '/api/translationRequest'
): ITranslationRequestService => ({
    get<T>(id: number): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/${id}`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getActiveTranslations<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/active`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    requests<T>(
        pageNumber: number,
        searchQuery: string,
        orderBy: string,
        ascending: boolean,
        statuses?: TranslationStatus[]
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/requests`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    orderBy: orderBy,
                    ascending: ascending,
                    ...(statuses && statuses.length > 0 ? { statuses } : {})
                })
            )
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    cancel<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/cancel`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    remove<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/remove`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    removeBulk<T>(request: IBulkRemoveRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/remove/bulk`, {
                ids: request.ids ?? [],
                allMatching: request.allMatching ?? false,
                searchQuery: request.searchQuery,
                statuses: request.statuses ?? [],
                excludeIds: request.excludeIds ?? []
            })
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    retry<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/retry`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    resume<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/resume`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    proofread<T>(translationRequest: ITranslationRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/proofread`, translationRequest)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    applyProofreadLine<T>(request: IProofreadLineApplyRequest): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/proofread/line`, request)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const translationRequestService = (axios: AxiosStatic): ITranslationRequestService => {
    return service(axios)
}
