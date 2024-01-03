export interface ILanguage {
    name: string
    code: string
    count: number
}

export interface ILanguageState {
    languages: ILanguage[]
    usedLanguages: ILanguage[] | null
}
