/// <reference types="vite/client" />
import { I18n } from '@/plugins/i18n'

declare module '@vue/runtime-core' {
    interface ComponentCustomProperties {
        translate: (key: string) => string
        $i18n: I18n
    }
}
