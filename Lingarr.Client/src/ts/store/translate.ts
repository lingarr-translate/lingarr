import { ILanguage } from '@/ts'

export interface IUseTranslateStore {
    languages: ILanguage[]
    languagesError: boolean
    languagesLoading: boolean
}
