export interface IUseInstanceStore {
    isOpen: boolean
    theme: ITheme
    poster: string
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
