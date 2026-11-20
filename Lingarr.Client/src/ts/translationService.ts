export interface ITranslationServiceInstance {
    id: number
    provider: string
    name: string
    position: number
}

export interface ICreateTranslationServiceRequest {
    provider: string
    name?: string
}

export interface IUpdateTranslationServiceRequest {
    name: string
}

export interface IReorderTranslationServicesRequest {
    ids: number[]
}

export interface ISetTranslationServiceSettingRequest {
    key: string
    value: string
}
