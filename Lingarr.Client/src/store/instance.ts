import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseInstanceStore, IStatus, STATUS, ITheme, THEME } from '@/ts/instance'
import { useLocalStorage } from '@/composables/useLocalStorage'

export const useInstanceStore = defineStore({
    id: 'instance',
    state: (): IUseInstanceStore => ({
        loading: false,
        status: STATUS.NONE,
        theme: THEME.DARK,
        menuIsOpen: false
    }),
    getters: {
        getMenuIsOpen: (state): boolean => state.menuIsOpen,
        getLoading: (state): boolean => state.loading,
        getStatus: (state): IStatus => state.status,
        getTheme: (state): ITheme => {
            const storage = useLocalStorage()
            const theme = storage.getItem<ITheme>('theme')

            if (theme) {
                return theme
            }
            return state.theme
        }
    },
    actions: {
        setMenuIsOpen(menuIsOpen: boolean): void {
            this.menuIsOpen = menuIsOpen
        },
        setLoading(status: boolean): void {
            this.loading = status
        },
        setStatus(status: IStatus): void {
            this.status = status
        },
        applyThemeOnLoad() {
            const storage = useLocalStorage()
            const theme = storage.getItem('theme')
            if (
                theme === THEME.DARK ||
                (!theme && window.matchMedia('(prefers-color-scheme: dark)').matches)
            ) {
                this.setTheme(THEME.DARK)
            } else {
                this.setTheme(THEME.LIGHT)
            }
        },
        setTheme(theme?: ITheme): void {
            const localStorage = useLocalStorage()

            this.theme = theme ?? (this.theme === THEME.DARK ? THEME.LIGHT : THEME.DARK)

            localStorage.setItem('theme', this.theme)

            document.documentElement.classList.add(this.theme)
            document.documentElement.classList.remove(
                this.theme === THEME.DARK ? THEME.LIGHT : THEME.DARK
            )
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useInstanceStore, import.meta.hot))
}
