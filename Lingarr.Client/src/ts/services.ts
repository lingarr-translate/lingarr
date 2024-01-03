export interface ILanguageService {
    list<T>(): Promise<T>
}

export interface IResourceService {
    list<T>(mediaType: string): Promise<T>
}

export interface ITranslateService {
    translate<T>(path: string, targetLanguage: string): Promise<T>
}
