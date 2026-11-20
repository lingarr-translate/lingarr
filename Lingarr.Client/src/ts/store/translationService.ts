import { ITranslationServiceInstance } from '@/ts'

export interface IUseTranslationServiceStore {
    services: ITranslationServiceInstance[]
    settings: Record<number, Record<string, string>>
    loaded: boolean
}
