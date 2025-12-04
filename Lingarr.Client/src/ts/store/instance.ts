import { IVersion } from '@/ts/version'

export interface IUseInstanceStore {
    version: IVersion
    isOpen: boolean
    theme: ITheme
    poster: string
    authenticated: boolean
}

export const THEMES = {
    SOLARIZED_LIGHT: 'solarized-light',
    SOLARIZED_DARK: 'solarized-dark',
    DRACULA: 'dracula',
    NORD: 'nord',
    MONOKAI: 'monokai',
    MATERIAL_DARK: 'material-dark',
    GOTHAM: 'gotham',
    GRUVBOX: 'gruvbox',
    CYBERPUNK_NEON: 'cyberpunk-neon',
    HORIZON: 'horizon',
    LINGARR: 'lingarr'
} as const

export type ITheme = (typeof THEMES)[keyof typeof THEMES]

export const LOCALE = {
    ENGLISH: 'en',
    DUTCH: 'nl'
} as const

export type ILocale = (typeof LOCALE)[keyof typeof LOCALE]

export type IFilter = {
    pageNumber: number
    searchQuery: string
    sortBy: string
    isAscending: boolean
}
export type IOptions = {
    label: string
    value: string
}
