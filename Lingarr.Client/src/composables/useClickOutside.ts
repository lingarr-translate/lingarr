import { onMounted, onBeforeUnmount, Ref } from 'vue'

export default function useClickOutside(
    component: Ref<HTMLElement | undefined>,
    callback: (() => void) | undefined,
    excludeComponent?: Ref<HTMLElement | undefined> | null
) {
    if (!component) {
        throw new Error('A target component has to be provided.')
    }

    if (!callback) {
        throw new Error('A callback has to be provided.')
    }

    const listener = (event: Event) => {
        if (
            event.target === component.value ||
            event.composedPath().includes(component.value as Node) ||
            event.target === excludeComponent?.value ||
            event.composedPath().includes(excludeComponent?.value as Node)
        ) {
            return
        }
        if (typeof callback === 'function') {
            callback()
        }
    }

    onMounted(() => {
        window.addEventListener('click', listener)
    })

    onBeforeUnmount(() => {
        window.removeEventListener('click', listener)
    })
}
